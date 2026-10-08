using NextTech.Application.Credentials;

namespace NextTech.Application.Interfaces;

public interface IWhatsAppNotificationSender
{
    Task SendRegistrationWelcomeAsync(
        string phone,
        string email,
        string nickname,
        CancellationToken ct);

    Task SendCredentialAsync(
        string phone,
        string nickname,
        BuyerCredentialDocument document,
        CancellationToken ct);
}
