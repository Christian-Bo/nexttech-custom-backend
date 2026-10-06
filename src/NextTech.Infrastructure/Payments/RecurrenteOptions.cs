namespace NextTech.Infrastructure.Payments;

public sealed class RecurrenteOptions
{
    public const string SectionName = "Recurrente";

    public bool Enabled { get; init; }
    public string BaseUrl { get; init; } = "https://app.recurrente.com";
    public string SecretKey { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
    public string SuccessUrl { get; init; } = string.Empty;
    public string CancelUrl { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
}
