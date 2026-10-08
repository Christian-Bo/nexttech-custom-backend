namespace NextTech.Infrastructure.WhatsApp;

public sealed class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiKeyHeader { get; init; } = "X-API-Key";
    public int TimeoutSeconds { get; init; } = 30;
}
