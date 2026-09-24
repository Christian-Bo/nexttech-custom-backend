using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NextTech.Application.Authentication;
using NextTech.Application.Profiles;

namespace NextTech.IntegrationTests;

public sealed class AuthorizationContractTests : IClassFixture<NextTechApiFactory>
{
    private readonly NextTechApiFactory _factory;

    public AuthorizationContractTests(NextTechApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task BuyerEndpoint_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/profile/notification-options");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BuyerEndpoint_WithActiveBuyer_Returns200AndFriendlyOptions()
    {
        using var client = CreateAuthorizedClient(_factory.CreateBuyerToken());

        var response = await client.GetAsync("/api/profile/notification-options");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var options = await response.Content.ReadFromJsonAsync<List<BuyerNotificationPreferenceOption>>();
        Assert.NotNull(options);
        Assert.Contains(options!, option => option.Code == "EMAIL" && option.Name == "Correo electrónico");
        Assert.Contains(options!, option => option.Code == "WHATSAPP" && option.Name == "WhatsApp");
    }

    [Fact]
    public async Task BuyerEndpoint_WithBlockedBuyer_Returns403()
    {
        using var client = CreateAuthorizedClient(_factory.CreateBuyerToken(blocked: true));

        var response = await client.GetAsync("/api/profile/notification-options");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BuyerEndpoint_WithInactiveBuyer_Returns403()
    {
        using var client = CreateAuthorizedClient(_factory.CreateBuyerToken(active: false));

        var response = await client.GetAsync("/api/profile/notification-options");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task BuyerEndpoint_WithInternalToken_Returns403()
    {
        using var client = CreateAuthorizedClient(_factory.CreateInternalToken());

        var response = await client.GetAsync("/api/profile/notification-options");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminEndpoint_WithAdminToken_ReturnsRolesWithCodeAndName()
    {
        using var client = CreateAuthorizedClient(_factory.CreateInternalToken(InternalRoles.Admin));

        var response = await client.GetAsync("/api/internal/roles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var roles = await response.Content.ReadFromJsonAsync<List<InternalRoleInfo>>();
        Assert.NotNull(roles);
        Assert.Contains(roles!, role => role.Code == InternalRoles.Admin && role.Name == "Administrador");
        Assert.Contains(roles!, role => role.Code == InternalRoles.Supervisor && role.Name == "Supervisor");
        Assert.Contains(roles!, role => role.Code == InternalRoles.DeliveryDriver && role.Name == "Repartidor");
    }

    [Fact]
    public async Task AdminEndpoint_WithSupervisorToken_Returns403()
    {
        using var client = CreateAuthorizedClient(_factory.CreateInternalToken(InternalRoles.Supervisor));

        var response = await client.GetAsync("/api/internal/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InternalPasswordChangeRequired_CanReadMeButCannotUseAdminEndpoints()
    {
        using var client = CreateAuthorizedClient(
            _factory.CreateInternalToken(InternalRoles.Admin, mustChangePassword: true));

        var me = await client.GetAsync("/api/internal/auth/me");
        var roles = await client.GetAsync("/api/internal/roles");

        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, roles.StatusCode);
    }

    [Fact]
    public async Task InternalToken_WithStaleRole_Returns403()
    {
        using var client = CreateAuthorizedClient(
            _factory.CreateStaleInternalToken(InternalRoles.Admin, InternalRoles.Supervisor));

        var response = await client.GetAsync("/api/internal/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task InternalToken_ForInactiveAccount_Returns403()
    {
        using var client = CreateAuthorizedClient(
            _factory.CreateInternalToken(InternalRoles.Admin, active: false));

        var response = await client.GetAsync("/api/internal/roles");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private HttpClient CreateAuthorizedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
