namespace NextTech.Application.Credentials;

public sealed record BuyerCredentialPdfData(
    long BuyerId,
    string Nickname,
    string Role,
    byte[] PortraitContent,
    string PortraitContentType,
    string QrCredential,
    DateTimeOffset IssuedAtUtc);

public sealed record BuyerCredentialDocument(
    byte[] Content,
    string ContentType,
    string FileName,
    DateTimeOffset IssuedAtUtc);
