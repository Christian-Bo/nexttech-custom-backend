using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NextTech.Application.Authentication;
using NextTech.Application.Interfaces;

namespace NextTech.IntegrationTests;

public sealed class NextTechApiFactory : WebApplicationFactory<Program>
{
    static NextTechApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__SqlServer",
            "Server=localhost,1433;Database=NextTechIntegrationTests;User Id=sa;Password=TestOnly_Passw0rd!;Encrypt=False;TrustServerCertificate=True");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Oracle",
            "User Id=test;Password=test;Data Source=localhost:1521/XEPDB1;");
        Environment.SetEnvironmentVariable("AllowedClientOrigins__0", "http://localhost:3000");
        Environment.SetEnvironmentVariable("Jwt__Issuer", "NextTech.IntegrationTests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "NextTech.IntegrationTests.Client");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "30");
        Environment.SetEnvironmentVariable(
            "Jwt__Key",
            "nexttech-integration-tests-only-signing-key-64-characters-minimum-2026");
        Environment.SetEnvironmentVariable("Smtp__Enabled", "false");
        Environment.SetEnvironmentVariable("FaceApi__BaseUrl", "https://localhost:9443");
        Environment.SetEnvironmentVariable("FaceApi__ApiKey", "integration-test-key");
        Environment.SetEnvironmentVariable("FaceApi__ApiKeyHeader", "X-Internal-Api-Key");
        Environment.SetEnvironmentVariable("FaceApi__TimeoutSeconds", "5");
    }

    public TestSecurityState SecurityState { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICentralIdentityGateway>();
            services.RemoveAll<IInternalAuthRepository>();
            services.RemoveAll<IInternalUserAdministrationRepository>();

            services.AddSingleton(SecurityState);
            services.AddSingleton<ICentralIdentityGateway>(SecurityState);
            services.AddSingleton<IInternalAuthRepository>(SecurityState);
            services.AddSingleton<IInternalUserAdministrationRepository>(SecurityState);
        });
    }

    public string CreateBuyerToken(bool active = true, bool blocked = false)
    {
        SecurityState.SetBuyer(active, blocked);
        var tokenService = Services.GetRequiredService<ITokenService>();
        var buyer = SecurityState.Buyer;

        return tokenService.CreateBuyerToken(new BuyerProfile(
            buyer.IdUsuario,
            buyer.Correo,
            buyer.Telefono,
            buyer.FechaNacimiento,
            buyer.Nickname,
            buyer.NotificaEmail,
            buyer.NotificaWhatsApp,
            buyer.Activo,
            buyer.Bloqueado)).AccessToken;
    }

    public string CreateInternalToken(
        string role = InternalRoles.Admin,
        bool active = true,
        bool mustChangePassword = false,
        DateTime? blockedUntil = null)
    {
        SecurityState.SetInternalUser(role, active, mustChangePassword, blockedUntil);
        var tokenService = Services.GetRequiredService<ITokenService>();
        return tokenService.CreateInternalToken(SecurityState.InternalUser).AccessToken;
    }

    public string CreateStaleInternalToken(string tokenRole, string currentRole)
    {
        SecurityState.SetInternalUser(currentRole);
        var tokenService = Services.GetRequiredService<ITokenService>();
        var tokenUser = SecurityState.InternalUser with { Role = tokenRole };
        return tokenService.CreateInternalToken(tokenUser).AccessToken;
    }
}
