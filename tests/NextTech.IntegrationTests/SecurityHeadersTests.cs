using System.Net;
using System.Net.Http.Headers;

namespace NextTech.IntegrationTests;

public sealed class SecurityHeadersTests : IClassFixture<NextTechApiFactory>
{
    private readonly NextTechApiFactory _factory;

    public SecurityHeadersTests(NextTechApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SensitiveAuthenticatedResponse_DisablesCachingAndAddsDefensiveHeaders()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateBuyerToken());

        var response = await client.GetAsync("/api/profile/notification-options");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("nosniff", response.Headers.GetValues("X-Content-Type-Options"));
        Assert.Contains("DENY", response.Headers.GetValues("X-Frame-Options"));
        Assert.Contains("no-referrer", response.Headers.GetValues("Referrer-Policy"));
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty);
    }

    [Fact]
    public async Task UnauthorizedSensitiveResponse_IsAlsoNoStore()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/profile/notification-options");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString() ?? string.Empty);
    }
}
