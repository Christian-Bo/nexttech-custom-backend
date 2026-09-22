using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NextTech.Infrastructure.Persistence.Oracle;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/dev/diagnostics")]
[ApiExplorerSettings(IgnoreApi = false)]
public sealed class DevelopmentDiagnosticsController(
    IHostEnvironment environment,
    IConfiguration configuration,
    OracleDbContext oracleDb) : ControllerBase
{
    [HttpGet("configuration")]
    public IActionResult ConfigurationSummary()
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var origins = configuration.GetSection("AllowedClientOrigins").Get<string[]>() ?? [];

        return Ok(new
        {
            environment = environment.EnvironmentName,
            configuration = new
            {
                oracleConfigured = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("Oracle")),
                sqlServerConfigured = !string.IsNullOrWhiteSpace(configuration.GetConnectionString("SqlServer")),
                jwtConfigured = !string.IsNullOrWhiteSpace(configuration["Jwt:Key"]),
                smtpEnabled = configuration.GetValue<bool>("Smtp:Enabled"),
                faceApiConfigured = !string.IsNullOrWhiteSpace(configuration["FaceApi:BaseUrl"]),
                allowedClientOrigins = origins.Where(static x => !string.IsNullOrWhiteSpace(x)).ToArray()
            },
            note = "Este endpoint nunca devuelve passwords, connection strings, JWT secrets ni API keys."
        });
    }

    [HttpGet("oracle")]
    public async Task<IActionResult> Oracle(CancellationToken ct)
    {
        if (!environment.IsDevelopment())
            return NotFound();

        var connection = oracleDb.Database.GetDbConnection();
        var startedAt = DateTimeOffset.UtcNow;

        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM TIENDA_APP.USUARIO";
        var countValue = await command.ExecuteScalarAsync(ct);
        var count = countValue is null || countValue == DBNull.Value ? 0L : Convert.ToInt64(countValue);

        return Ok(new
        {
            status = "Healthy",
            database = "Oracle",
            connectionState = connection.State.ToString(),
            userTableAccessible = true,
            usuarioCount = count,
            elapsedMs = Math.Max(0, (long)(DateTimeOffset.UtcNow - startedAt).TotalMilliseconds),
            traceId = HttpContext.TraceIdentifier
        });
    }
}
