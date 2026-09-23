using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextTech.Application.Interfaces;
using NextTech.Infrastructure.Authentication;
using NextTech.Infrastructure.Credentials;
using NextTech.Infrastructure.Email;
using NextTech.Infrastructure.Face;
using NextTech.Infrastructure.Health;
using NextTech.Infrastructure.Persistence.Oracle;
using NextTech.Infrastructure.Persistence.Oracle.Repositories;
using NextTech.Infrastructure.Persistence.SqlServer;
using NextTech.Infrastructure.Persistence.SqlServer.Repositories;
using QuestPDF.Infrastructure;

namespace NextTech.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var sqlServerConnection = configuration.GetConnectionString("SqlServer");
        var oracleConnection = configuration.GetConnectionString("Oracle");

        if (string.IsNullOrWhiteSpace(sqlServerConnection))
            throw new InvalidOperationException("Falta ConnectionStrings:SqlServer. Configure User Secrets o variable ConnectionStrings__SqlServer.");
        if (string.IsNullOrWhiteSpace(oracleConnection))
            throw new InvalidOperationException("Falta ConnectionStrings:Oracle. Configure User Secrets o variable ConnectionStrings__Oracle.");

        services.AddDbContext<NextTechDbContext>(options =>
            options.UseSqlServer(sqlServerConnection));

        services.AddDbContext<OracleDbContext>(options =>
            options.UseOracle(oracleConnection));

        services.AddScoped<ICompradorCentralReader, CompradorCentralReader>();
        services.AddScoped<ICentralIdentityGateway, OracleCentralIdentityGateway>();
        services.AddScoped<IInternalAuthRepository, InternalAuthRepository>();
        services.AddScoped<IBuyerFaceEnrollmentStore, OracleBuyerBiometricStore>();
        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        QuestPDF.Settings.License = LicenseType.Community;
        services.AddSingleton<IBuyerCredentialPdfGenerator, BuyerCredentialPdfGenerator>();

        services.AddOptions<SmtpOptions>()
            .Bind(configuration.GetSection(SmtpOptions.SectionName))
            .Validate(
                static options => !options.Enabled ||
                    (!string.IsNullOrWhiteSpace(options.Host) &&
                     options.Port is > 0 and <= 65535 &&
                     !string.IsNullOrWhiteSpace(options.UserName) &&
                     !string.IsNullOrWhiteSpace(options.Password) &&
                     !string.IsNullOrWhiteSpace(options.From) &&
                     options.TimeoutSeconds is >= 5 and <= 120 &&
                     Uri.TryCreate(options.RecoveryUrlBase, UriKind.Absolute, out var recoveryUri) &&
                     (recoveryUri.Scheme == Uri.UriSchemeHttp || recoveryUri.Scheme == Uri.UriSchemeHttps)),
                "Smtp:Enabled=true requiere Host, Port, UserName, Password, From, TimeoutSeconds y RecoveryUrlBase http/https válidos.")
            .ValidateOnStart();
        services.AddSingleton<SmtpNotificationSender>();
        services.AddSingleton<IRegistrationNotificationSender>(sp => sp.GetRequiredService<SmtpNotificationSender>());
        services.AddSingleton<IRecoveryNotificationSender>(sp => sp.GetRequiredService<SmtpNotificationSender>());
        services.AddSingleton<IBuyerCredentialNotificationSender>(sp => sp.GetRequiredService<SmtpNotificationSender>());

        services.AddOptions<FaceApiOptions>()
            .Bind(configuration.GetSection(FaceApiOptions.SectionName))
            .Validate(static options =>
                Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri) &&
                (baseUri.Scheme == Uri.UriSchemeHttp || baseUri.Scheme == Uri.UriSchemeHttps),
                "FaceApi:BaseUrl debe ser una URL http/https absoluta.")
            .Validate(static options =>
                IsRelativeApiPath(options.EnrollPath) &&
                IsRelativeApiPath(options.SegmentPath) &&
                IsRelativeApiPath(options.LivenessPath) &&
                IsRelativeApiPath(options.VerifyPath),
                "Las rutas de FaceApi deben comenzar con '/'.")
            .Validate(static options =>
                !string.IsNullOrWhiteSpace(options.ApiKey) &&
                !string.IsNullOrWhiteSpace(options.ApiKeyHeader),
                "FaceApi requiere ApiKey y ApiKeyHeader configurados.")
            .Validate(static options => options.TimeoutSeconds is >= 5 and <= 120,
                "FaceApi:TimeoutSeconds debe estar entre 5 y 120 segundos.")
            .ValidateOnStart();

        var faceOptions = configuration.GetSection(FaceApiOptions.SectionName).Get<FaceApiOptions>() ?? new FaceApiOptions();
        services.AddHttpClient<IFaceBiometricService, ExternalFaceBiometricService>(client =>
        {
            client.BaseAddress = new Uri(faceOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(faceOptions.TimeoutSeconds);
            if (!string.IsNullOrWhiteSpace(faceOptions.ApiKey) && !string.IsNullOrWhiteSpace(faceOptions.ApiKeyHeader))
                client.DefaultRequestHeaders.TryAddWithoutValidation(faceOptions.ApiKeyHeader, faceOptions.ApiKey);
        });

        services.AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>(
                "sqlserver",
                tags: ["ready", "database"])
            .AddCheck<OracleHealthCheck>(
                "oracle",
                tags: ["ready", "database"]);

        services.AddJwtAuthentication(configuration);

        return services;
    }

    private static bool IsRelativeApiPath(string value)
        => !string.IsNullOrWhiteSpace(value) &&
           value.StartsWith("/", StringComparison.Ordinal) &&
           !value.StartsWith("//", StringComparison.Ordinal);

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key debe configurarse con al menos 32 caracteres.");

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwt.Key)),
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        return services;
    }
}
