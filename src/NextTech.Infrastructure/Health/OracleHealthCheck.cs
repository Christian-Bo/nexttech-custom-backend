using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Oracle.ManagedDataAccess.Client;

namespace NextTech.Infrastructure.Health;

public sealed class OracleHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("Oracle");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return HealthCheckResult.Unhealthy("ConnectionStrings:Oracle no está configurada.");
        }

        try
        {
            await using var connection = new OracleConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return HealthCheckResult.Healthy("Oracle disponible.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("No fue posible conectar con Oracle.", ex);
        }
    }
}
