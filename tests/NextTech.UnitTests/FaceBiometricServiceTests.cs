using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using NextTech.Application.Common;
using NextTech.Application.Face;
using NextTech.Infrastructure.Face;

namespace NextTech.UnitTests;

public sealed class FaceBiometricServiceTests
{
    [Fact]
    public async Task Challenge_ParsesProviderResponse()
    {
        using var client = CreateClient((request, _) =>
        {
            Assert.Equal("/api/v1/faces/liveness/challenge", request.RequestUri?.AbsolutePath);
            return Json(HttpStatusCode.OK, """
            {
              "status":"ok",
              "challenge":{
                "id":"challenge-123",
                "action":"TURN_IMAGE_RIGHT",
                "instruction":"Turn right",
                "expiresAtUtc":"2026-09-21T05:37:00+00:00",
                "expiresInSeconds":60
              }
            }
            """);
        });

        var result = await CreateService(client).CreateLivenessChallengeAsync(CancellationToken.None);

        Assert.Equal("challenge-123", result.Id);
        Assert.Equal("TURN_IMAGE_RIGHT", result.Action);
        Assert.Equal(60, result.ExpiresInSeconds);
    }

    [Fact]
    public async Task Enroll_SendsExactImageFieldAndParsesProtectedArtifacts()
    {
        using var client = CreateClient(async (request, ct) =>
        {
            Assert.Equal("/api/v1/faces/enroll", request.RequestUri?.AbsolutePath);
            var body = await request.Content!.ReadAsStringAsync(ct);
            Assert.Contains("name=image", body);
            Assert.DoesNotContain("subject_id", body);

            return Json(HttpStatusCode.OK, """
            {
              "mensaje":"ok",
              "portrait":{
                "mimeType":"image/png",
                "imageBase64":"AQIDBA==",
                "width":600,
                "height":800,
                "background":"transparent"
              },
              "biometricTemplate":{
                "value":"protected-template",
                "version":"1",
                "keyId":"prod-v1",
                "model":"SFace",
                "dimensions":128,
                "embeddingSha256":"ABC"
              },
              "faceScore":0.84,
              "quality":{"status":"ACCEPTED_WITH_WARNINGS","issues":[{"code":"WARN"}]}
            }
            """);
        });

        var result = await CreateService(client).CreateTemplateAsync(Image(), CancellationToken.None);

        Assert.Equal("protected-template", result.BiometricTemplate.Value);
        Assert.Equal("SFace", result.BiometricTemplate.Model);
        Assert.Equal(128, result.BiometricTemplate.Dimensions);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, result.Portrait.Content);
        Assert.Equal(600, result.Portrait.Width);
        Assert.Equal(1, result.WarningCount);
    }

    [Fact]
    public async Task Verify_SendsExactFieldsAndParsesAuthenticatedResponse()
    {
        using var client = CreateClient(async (request, ct) =>
        {
            Assert.Equal("/api/v1/faces/verify-template-live", request.RequestUri?.AbsolutePath);
            var body = await request.Content!.ReadAsStringAsync(ct);
            Assert.Contains("name=biometricTemplate", body);
            Assert.Contains("protected-template", body);
            Assert.Contains("name=challengeId", body);
            Assert.Contains("challenge-1", body);
            Assert.Contains("name=neutralImage", body);
            Assert.Contains("name=challengeImage", body);

            return Json(HttpStatusCode.OK, """
            {
              "mensaje":"ok",
              "authenticationPassed":true,
              "decision":"AUTHENTICATED",
              "liveness":{
                "isLive":true,
                "decision":"LIVE",
                "reasonCode":"LIVENESS_PASSED",
                "action":"TURN_IMAGE_RIGHT",
                "yawDelta":0.268,
                "requiredYawDelta":0.18,
                "frameIdentitySimilarity":0.762,
                "frameIdentityThreshold":0.3
              },
              "verification":{
                "isMatch":true,
                "similarity":1.0,
                "threshold":0.393,
                "decisionMargin":0.607
              }
            }
            """);
        });

        var result = await CreateService(client).VerifyTemplateLiveAsync(
            "protected-template",
            "challenge-1",
            Image("neutral.jpg"),
            Image("challenge.jpg"),
            CancellationToken.None);

        Assert.True(result.AuthenticationPassed);
        Assert.True(result.IsLive);
        Assert.True(result.IsMatch);
        Assert.Equal("AUTHENTICATED", result.Decision);
        Assert.Equal(0.393m, result.Threshold);
    }

    [Fact]
    public async Task Verify_ParsesHttp200LivenessFailureAsFailedAuthentication()
    {
        using var client = CreateClient((_, _) => Json(HttpStatusCode.OK, """
        {
          "authenticationPassed":false,
          "decision":"LIVENESS_FAILED",
          "liveness":{
            "isLive":false,
            "decision":"NOT_LIVE",
            "reasonCode":"LIVENESS_ACTION_NOT_SATISFIED",
            "action":"TURN_IMAGE_RIGHT",
            "yawDelta":-0.12,
            "requiredYawDelta":0.18,
            "frameIdentitySimilarity":0,
            "frameIdentityThreshold":0.3
          },
          "verification":null
        }
        """));

        var result = await CreateService(client).VerifyTemplateLiveAsync(
            "protected-template", "challenge-1", Image(), Image(), CancellationToken.None);

        Assert.False(result.AuthenticationPassed);
        Assert.False(result.IsLive);
        Assert.Null(result.IsMatch);
        Assert.Equal("LIVENESS_ACTION_NOT_SATISFIED", result.LivenessReasonCode);
    }

    [Fact]
    public async Task Verify_PreservesProviderChallengeErrorCode()
    {
        using var client = CreateClient((_, _) => Json(HttpStatusCode.BadRequest, """
        {
          "detail":"El challenge no puede utilizarse para continuar el login facial.",
          "errors":{"challengeId":["El challenge ya fue utilizado."]},
          "code":"LIVENESS_CHALLENGE_INVALID_OR_USED"
        }
        """, "application/problem+json"));

        var ex = await Assert.ThrowsAsync<AppProviderRejectedException>(() =>
            CreateService(client).VerifyTemplateLiveAsync(
                "protected-template", "challenge-1", Image(), Image(), CancellationToken.None));

        Assert.Equal(400, ex.StatusCode);
        Assert.Equal("LIVENESS_CHALLENGE_INVALID_OR_USED", ex.ErrorCode);
    }

    [Fact]
    public async Task Segment_ReturnsBinaryImage()
    {
        var expected = new byte[] { 10, 20, 30, 40 };
        using var client = CreateClient((request, _) =>
        {
            Assert.Equal("/api/v1/faces/segment/image", request.RequestUri?.AbsolutePath);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(expected)
            };
            response.Content.Headers.ContentType = new("image/png");
            response.Content.Headers.ContentDisposition = new("attachment") { FileName = "portrait-segmented.png" };
            return response;
        });

        var result = await CreateService(client).SegmentForCardAsync(Image(), CancellationToken.None);

        Assert.Equal(expected, result.Content);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal("portrait-segmented.png", result.FileName);
    }

    [Fact]
    public void ImageValidator_RejectsSpoofedMimeType()
    {
        var image = new FaceImage([1, 2, 3, 4], "image/jpeg", "not-a-real-image.jpg");

        Assert.Throws<AppValidationException>(() => FaceImageValidator.Validate(image));
    }

    [Fact]
    public void ImageValidator_AcceptsValidJpegSignature()
    {
        var image = Image();
        FaceImageValidator.Validate(image);
    }

    [Fact]
    public async Task IdentityEnroll_SendsSubjectIdInSnakeCaseAndOriginalImage()
    {
        using var client = CreateClient(async (request, ct) =>
        {
            Assert.Equal("/api/v1/faces/identity/enroll", request.RequestUri?.AbsolutePath);
            var body = await request.Content!.ReadAsStringAsync(ct);
            Assert.Contains("name=subject_id", body);
            Assert.Contains("162", body);
            Assert.Contains("name=image", body);
            Assert.DoesNotContain("name=subjectId", body);
            Assert.DoesNotContain("name=neutralImage", body);
            return Json(HttpStatusCode.OK, """{"enrolled":true}""");
        });

        await CreateService(client, enabled: true).RegisterIdentityAsync(162, Image(), CancellationToken.None);
    }

    [Fact]
    public async Task Identify_SendsOnlyImageAndParsesProviderTimings()
    {
        using var client = CreateClient(async (request, ct) =>
        {
            Assert.Equal("/api/v1/faces/identify", request.RequestUri?.AbsolutePath);
            Assert.Equal(HttpMethod.Post, request.Method);
            var form = await request.Content!.ReadAsStringAsync(ct);
            Assert.Contains("name=image", form);
            Assert.DoesNotContain("name=subject_id", form);
            Assert.DoesNotContain("name=challengeId", form);
            Assert.DoesNotContain("name=neutralImage", form);
            Assert.DoesNotContain("name=challengeImage", form);
            return Json(HttpStatusCode.OK, """
            {"identified":true,"subjectId":"162","similarity":0.609870221,
             "threshold":0.50,"livenessPassed":null,
             "processing":{"embeddingMs":25,"searchMs":0.0225,"totalMs":328},
             "traceId":"face-trace-123"}
            """);
        });

        var result = await CreateService(client, enabled: true).IdentifyAsync(Image(), CancellationToken.None);

        Assert.True(result.Identified);
        Assert.Equal("162", result.SubjectId);
        Assert.Equal(0.50m, result.Threshold);
        Assert.Equal(328d, result.FaceApiProcessingMs.GetValueOrDefault());
        Assert.Equal(0.0225d, result.FaceApiSearchMs.GetValueOrDefault());
        Assert.Equal(25d, result.FaceApiEmbeddingMs.GetValueOrDefault());
        Assert.True(result.FaceApiHttpMs >= 0);
        Assert.Equal("face-trace-123", result.FaceApiTraceId);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "FACE_NOT_IDENTIFIED", 401)]
    [InlineData(HttpStatusCode.UnprocessableEntity, "FACE_IDENTIFICATION_AMBIGUOUS", 422)]
    public async Task Identify_ReturnsSafeProblemWithExpectedCode(
        HttpStatusCode status, string code, int expectedStatus)
    {
        using var client = CreateClient((_, _) => Json(status,
            $$"""{"code":"{{code}}","detail":"Internal candidate data","traceId":"face-123"}""",
            "application/problem+json"));

        var exception = await Assert.ThrowsAsync<AppFaceIdentificationException>(
            () => CreateService(client, enabled: true).IdentifyAsync(Image(), CancellationToken.None));
        Assert.Equal(expectedStatus, exception.StatusCode);
        Assert.Equal(code, exception.ErrorCode);
        Assert.DoesNotContain("candidate", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("face-123", exception.FaceApiTraceId);
    }

    [Fact]
    public async Task Identify_InvalidProviderKey_IsDependencyFailureNotUnknownBuyer()
    {
        using var client = CreateClient((_, _) => Json(HttpStatusCode.Unauthorized,
            """{"code":"INVALID_API_KEY"}""", "application/problem+json"));

        await Assert.ThrowsAsync<AppDependencyException>(() =>
            CreateService(client, enabled: true).IdentifyAsync(Image(), CancellationToken.None));
    }

    [Fact]
    public async Task Identify_Disabled_DoesNotCallFaceApi()
    {
        var called = false;
        using var client = CreateClient((_, _) =>
        {
            called = true;
            return Json(HttpStatusCode.OK, "{}");
        });
        await Assert.ThrowsAsync<AppConflictException>(() =>
            CreateService(client).IdentifyAsync(Image(), CancellationToken.None));
        Assert.False(called);
    }

    [Fact]
    public async Task Identify_ForwardsCancellationToken()
    {
        using var client = CreateClient(async (_, ct) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, ct);
            throw new InvalidOperationException("Should have cancelled");
        });
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(client, enabled: true).IdentifyAsync(Image(), cancellation.Token));
    }

    [Fact]
    public async Task Identify_MalformedSuccessBody_FailsClosed()
    {
        using var client = CreateClient((_, _) => Json(HttpStatusCode.OK, """{"subjectId":"162"}"""));
        await Assert.ThrowsAsync<AppDependencyException>(() =>
            CreateService(client, enabled: true).IdentifyAsync(Image(), CancellationToken.None));
    }

    private static ExternalFaceBiometricService CreateService(HttpClient client, bool enabled = false)
        => new(client, Options.Create(new FaceApiOptions
        {
            BaseUrl = "https://face.example.test",
            IdentificationEnabled = enabled
        }));

    private static FaceImage Image(string fileName = "face.jpg")
        => new([0xFF, 0xD8, 0xFF, 0xE0, 1, 2, 3], "image/jpeg", fileName);

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        => new(new StubHttpMessageHandler((request, ct) => Task.FromResult(responder(request, ct))))
        {
            BaseAddress = new Uri("https://face.example.test")
        };

    private static HttpClient CreateClient(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        => new(new StubHttpMessageHandler(responder))
        {
            BaseAddress = new Uri("https://face.example.test")
        };

    private static HttpResponseMessage Json(HttpStatusCode status, string json, string mediaType = "application/json")
        => new(status)
        {
            Content = new StringContent(json, Encoding.UTF8, mediaType)
        };

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => responder(request, cancellationToken);
    }
}
