namespace NextTech.Api.Configuration;

/// <summary>
/// Loads the repository .env file for local Development executions (dotnet run / VS Code)
/// and maps the human-friendly variables to ASP.NET Core configuration keys.
/// Real process/environment variables always take precedence.
/// </summary>
internal static class LocalEnvLoader
{
    public static void Load()
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Development";

        if (!string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            return;

        var envPath = FindEnvFile();
        if (envPath is null)
            return;

        LoadFile(envPath);
        MapApplicationConfiguration();
    }

    private static string? FindEnvFile()
    {
        var candidates = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var start in candidates)
        {
            var directory = new DirectoryInfo(start);

            while (directory is not null)
            {
                var candidate = Path.Combine(directory.FullName, ".env");
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }
        }

        return null;
    }

    private static void LoadFile(string envPath)
    {
        foreach (var rawLine in File.ReadLines(envPath))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0)
                continue;

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            SetIfMissing(key, value);
        }
    }

    private static void MapApplicationConfiguration()
    {
        var sqlPassword = Get("SQLSERVER_SA_PASSWORD");
        var sqlDatabase = Get("SQLSERVER_DATABASE");

        if (!string.IsNullOrWhiteSpace(sqlPassword) &&
            !string.IsNullOrWhiteSpace(sqlDatabase) &&
            string.IsNullOrWhiteSpace(Get("ConnectionStrings__SqlServer")))
        {
            SetIfMissing(
                "ConnectionStrings__SqlServer",
                $"Server=localhost,1433;Database={sqlDatabase};User Id=sa;Password={sqlPassword};TrustServerCertificate=True;Encrypt=True");
        }

        var oracleUser = Get("ORACLE_USER");
        var oraclePassword = Get("ORACLE_PASSWORD");
        var oracleDataSource = Get("ORACLE_DATA_SOURCE");

        if (!string.IsNullOrWhiteSpace(oracleUser) &&
            !string.IsNullOrWhiteSpace(oraclePassword) &&
            !string.IsNullOrWhiteSpace(oracleDataSource) &&
            string.IsNullOrWhiteSpace(Get("ConnectionStrings__Oracle")))
        {
            SetIfMissing(
                "ConnectionStrings__Oracle",
                $"User Id={oracleUser};Password={oraclePassword};Data Source={oracleDataSource};");
        }

        Map("JWT_ISSUER", "Jwt__Issuer");
        Map("JWT_AUDIENCE", "Jwt__Audience");
        Map("JWT_EXPIRATION_MINUTES", "Jwt__ExpirationMinutes");
        Map("JWT_SECRET", "Jwt__Key");

        for (var i = 0; i < 20; i++)
            Map($"FRONTEND_ORIGIN_{i}", $"AllowedClientOrigins__{i}");

        Map("SMTP_ENABLED", "Smtp__Enabled");
        Map("SMTP_HOST", "Smtp__Host");
        Map("SMTP_PORT", "Smtp__Port");
        Map("SMTP_USERNAME", "Smtp__UserName");
        Map("SMTP_PASSWORD", "Smtp__Password");
        Map("SMTP_FROM", "Smtp__From");
        Map("SMTP_ENABLE_SSL", "Smtp__EnableSsl");
        Map("SMTP_TIMEOUT_SECONDS", "Smtp__TimeoutSeconds");
        Map("SMTP_RECOVERY_URL_BASE", "Smtp__RecoveryUrlBase");

        Map("FACE_API_BASE_URL", "FaceApi__BaseUrl");
        Map("FACE_API_ENROLL_PATH", "FaceApi__EnrollPath");
        Map("FACE_API_SEGMENT_PATH", "FaceApi__SegmentPath");
        Map("FACE_API_LIVENESS_PATH", "FaceApi__LivenessPath");
        Map("FACE_API_VERIFY_PATH", "FaceApi__VerifyPath");
        Map("FACE_API_KEY", "FaceApi__ApiKey");
        Map("FACE_API_KEY_HEADER", "FaceApi__ApiKeyHeader");
        Map("FACE_API_TIMEOUT_SECONDS", "FaceApi__TimeoutSeconds");
    }

    private static void Map(string source, string target)
    {
        var value = Get(source);
        if (!string.IsNullOrWhiteSpace(value))
            SetIfMissing(target, value);
    }

    private static string? Get(string key)
        => Environment.GetEnvironmentVariable(key);

    private static void SetIfMissing(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(key)))
            Environment.SetEnvironmentVariable(key, value, EnvironmentVariableTarget.Process);
    }
}
