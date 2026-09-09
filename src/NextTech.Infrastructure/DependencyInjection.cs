using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NextTech.Application.Interfaces;
using NextTech.Infrastructure.Authentication;
using NextTech.Infrastructure.Health;
using NextTech.Infrastructure.Persistence.Oracle;
using NextTech.Infrastructure.Persistence.Oracle.Repositories;
using NextTech.Infrastructure.Persistence.SqlServer;

namespace NextTech.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var sqlServerConnection =
            configuration.GetConnectionString("SqlServer") ?? string.Empty;

        var oracleConnection =
            configuration.GetConnectionString("Oracle") ?? string.Empty;

        services.AddDbContext<NextTechDbContext>(options =>
            options.UseSqlServer(sqlServerConnection));

        services.AddDbContext<OracleDbContext>(options =>
            options.UseOracle(oracleConnection));

        services.AddScoped<ICompradorCentralReader, CompradorCentralReader>();

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

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>() ?? new JwtOptions();

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

        services.AddAuthorization();

        return services;
    }
}
