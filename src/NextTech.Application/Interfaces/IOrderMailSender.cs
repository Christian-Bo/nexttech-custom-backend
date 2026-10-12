namespace NextTech.Application.Interfaces;

public interface IOrderMailSender
{
    Task<bool> SendPurchaseConfirmationAsync(
        string email,
        string nickname,
        string codigoOrden,
        byte[] pdf,
        CancellationToken cancellationToken);

    Task<bool> SendOrderReadyAsync(
        string email,
        string nickname,
        string codigoOrden,
        CancellationToken cancellationToken);

    Task<bool> SendOrderOutForDeliveryAsync(
        string email,
        string nickname,
        string codigoOrden,
        CancellationToken cancellationToken);

    Task<bool> SendDeliveryConfirmedAsync(
        string email,
        string nickname,
        string codigoOrden,
        CancellationToken cancellationToken);

    Task<bool> SendDeliveryIssueAsync(
        string email,
        string nickname,
        string codigoOrden,
        string reason,
        CancellationToken cancellationToken);
}
