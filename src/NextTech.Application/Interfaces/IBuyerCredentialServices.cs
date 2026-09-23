using NextTech.Application.Credentials;

namespace NextTech.Application.Interfaces;

public interface IBuyerCredentialPdfGenerator
{
    BuyerCredentialDocument Generate(BuyerCredentialPdfData data);
}

public interface IBuyerCredentialNotificationSender
{
    Task SendCredentialAsync(
        string email,
        string nickname,
        BuyerCredentialDocument document,
        CancellationToken ct);
}
