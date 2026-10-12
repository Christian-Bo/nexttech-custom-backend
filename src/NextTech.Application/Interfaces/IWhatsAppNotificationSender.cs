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

    Task SendPasswordRecoveryAsync(
        string phone,
        string nickname,
        string rawToken,
        DateTimeOffset expiresAt,
        CancellationToken ct);

    Task SendPasswordChangedAsync(
        string phone,
        string nickname,
        CancellationToken ct);

    Task<bool> SendPurchaseConfirmationAsync(
        string phone,
        string nickname,
        string orderCode,
        decimal total,
        string paymentStatus,
        byte[] receiptPdf,
        CancellationToken ct);

    Task<bool> SendOrderReadyAsync(
        string phone,
        string nickname,
        string orderCode,
        CancellationToken ct);

    Task<bool> SendOrderOutForDeliveryAsync(
        string phone,
        string nickname,
        string orderCode,
        CancellationToken ct);

    Task<bool> SendDeliveryConfirmedAsync(
        string phone,
        string nickname,
        string orderCode,
        CancellationToken ct);

    Task<bool> SendDeliveryIssueAsync(
        string phone,
        string nickname,
        string orderCode,
        string reason,
        CancellationToken ct);
}
