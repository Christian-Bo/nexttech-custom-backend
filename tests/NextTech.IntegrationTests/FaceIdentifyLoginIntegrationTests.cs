using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NextTech.Application.Common;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;

namespace NextTech.IntegrationTests;

public sealed class FaceIdentifyLoginIntegrationTests
{
    [Fact]
    public async Task QuickLogin_ValidBuyer_ReturnsNormalBuyerJwtWithoutProviderDiagnostics()
    {
        using var root = new NextTechApiFactory();
        using var factory = BuildFactory(root, new StubFaceService());
        using var client = factory.CreateClient();

        using var response = await PostImageAsync(client);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("accessToken", body);
        Assert.Contains("buyer", body);
        Assert.DoesNotContain("subjectId", body);
        Assert.DoesNotContain("faceApiSearchMs", body);
        Assert.DoesNotContain("similarity", body);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task QuickLogin_InactiveOrBlockedBuyer_Returns401WithoutToken(bool active, bool blocked)
    {
        using var root = new NextTechApiFactory();
        root.SecurityState.SetBuyer(active, blocked);
        using var factory = BuildFactory(root, new StubFaceService());
        using var client = factory.CreateClient();

        using var response = await PostImageAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("accessToken", body);
        Assert.DoesNotContain("nickname", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QuickLogin_WithoutLocalFaceEnrollment_Returns401()
    {
        using var root = new NextTechApiFactory();
        root.SecurityState.SetBuyerWithoutFaceEnrollment();
        using var factory = BuildFactory(root, new StubFaceService());
        using var client = factory.CreateClient();

        using var response = await PostImageAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task QuickLogin_SimilarityBelowThreshold_Returns401()
    {
        using var root = new NextTechApiFactory();
        using var factory = BuildFactory(root, new StubFaceService { Similarity = 0.30m });
        using var client = factory.CreateClient();

        using var response = await PostImageAsync(client);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(401, "FACE_NOT_IDENTIFIED")]
    [InlineData(422, "FACE_IDENTIFICATION_AMBIGUOUS")]
    public async Task QuickLogin_ProviderRejects_ProducesSafeProblemDetails(int status, string code)
    {
        using var root = new NextTechApiFactory();
        var provider = new StubFaceService
        {
            Error = new AppFaceIdentificationException(code, "No fue posible autenticar.", status, "face-trace-001")
        };
        using var factory = BuildFactory(root, provider);
        using var client = factory.CreateClient();

        using var response = await PostImageAsync(client);

        Assert.Equal((HttpStatusCode)status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        Assert.Equal(code, json.GetProperty("title").GetString());
        Assert.Equal("face-trace-001", json.GetProperty("faceApiTraceId").GetString());
        Assert.True(json.TryGetProperty("traceId", out _));
        Assert.False(json.TryGetProperty("accessToken", out _));
    }

    [Fact]
    public async Task QuickLogin_Disabled_Returns409AndNeverCallsFaceApi()
    {
        using var root = new NextTechApiFactory();
        var provider = new StubFaceService { Enabled = false };
        using var factory = BuildFactory(root, provider);
        using var client = factory.CreateClient();

        using var response = await PostImageAsync(client);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False(provider.WasCalled);
    }

    [Fact]
    public async Task QuickLogin_MissingImage_Returns400()
    {
        using var root = new NextTechApiFactory();
        using var factory = BuildFactory(root, new StubFaceService());
        using var client = factory.CreateClient();
        using var form = new MultipartFormDataContent();

        using var response = await client.PostAsync("/api/auth/face-identify-login", form);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("0")]
    [InlineData("999999")]
    public async Task QuickLogin_InvalidOrNonexistentSubjectId_Returns401(string subjectId)
    {
        using var root = new NextTechApiFactory();
        using var factory = BuildFactory(root, new StubFaceService { SubjectId = subjectId });
        using var client = factory.CreateClient();

        using var response = await PostImageAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> BuildFactory(
        NextTechApiFactory root, StubFaceService face)
        => root.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFaceBiometricService>();
            services.AddSingleton<IFaceBiometricService>(face);
        }));

    private static async Task<HttpResponseMessage> PostImageAsync(HttpClient client)
    {
        using var form = new MultipartFormDataContent();
        var image = new ByteArrayContent([0xff, 0xd8, 0xff, 0xe0, 1, 2, 3]);
        image.Headers.ContentType = new("image/jpeg");
        form.Add(image, "image", "neutral.jpg");
        return await client.PostAsync("/api/auth/face-identify-login", form);
    }

    private sealed class StubFaceService : IFaceBiometricService
    {
        public bool Enabled { get; init; } = true;
        public string SubjectId { get; init; } = "46";
        public decimal Similarity { get; init; } = 0.609870m;
        public Exception? Error { get; init; }
        public bool WasCalled { get; private set; }
        public bool IdentificationEnabled => Enabled;

        public Task<FaceQuickIdentificationResult> IdentifyAsync(FaceImage image, CancellationToken ct)
        {
            WasCalled = true;
            if (Error is not null)
                return Task.FromException<FaceQuickIdentificationResult>(Error);
            return Task.FromResult(new FaceQuickIdentificationResult(
                true, SubjectId, Similarity, 0.5m, 328, 0.0225, 25, 380, "face-trace-001"));
        }

        public Task<FaceChallengeResult> CreateLivenessChallengeAsync(CancellationToken ct)
            => throw new NotSupportedException();
        public Task<ProtectedFaceEnrollmentResult> CreateTemplateAsync(FaceImage image, CancellationToken ct)
            => throw new NotSupportedException();
        public Task<FaceVerificationResult> VerifyTemplateLiveAsync(string template, string challengeId,
            FaceImage neutralImage, FaceImage challengeImage, CancellationToken ct)
            => throw new NotSupportedException();
        public Task RegisterIdentityAsync(long buyerId, FaceImage referenceImage, CancellationToken ct)
            => throw new NotSupportedException();
        public Task<FaceIdentificationResult> IdentifyLiveAsync(string challengeId, FaceImage neutralImage,
            FaceImage challengeImage, CancellationToken ct)
            => throw new NotSupportedException();
        public Task<FaceSegmentationResult> SegmentForCardAsync(FaceImage image, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
