namespace NextTech.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public bool Enabled { get; init; }
    public string Host { get; init; } = "smtp.gmail.com";
    public int Port { get; init; } = 587;
    public string UserName { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string From { get; init; } = string.Empty;
    public bool EnableSsl { get; init; } = true;
    public int TimeoutSeconds { get; init; } = 20;
    public string RecoveryUrlBase { get; init; } = string.Empty;
}
