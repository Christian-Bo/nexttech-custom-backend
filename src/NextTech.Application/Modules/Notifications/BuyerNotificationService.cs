using NextTech.Application.Common;
using NextTech.Application.Interfaces;
using NextTech.Application.Notifications;

namespace NextTech.Application.Modules.Notifications;

public sealed class BuyerNotificationService(
    ICentralIdentityGateway gateway,
    IRegistrationNotificationSender emailNotifications,
    IWhatsAppNotificationSender whatsAppNotifications)
{
    public async Task<NotificationDispatchResult> ResendWelcomeAsync(
        long buyerId,
        NotificationDeliveryRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var channels = NotificationDeliveryChannels.Parse(request.Channel);

        var buyer = await gateway.FindByIdAsync(buyerId, ct)
            ?? throw new AppNotFoundException("Comprador no encontrado.");

        if (!buyer.Activo)
            throw new AppForbiddenException("La cuenta está inactiva.");
        if (buyer.Bloqueado)
            throw new AppForbiddenException("La cuenta está bloqueada.");

        if (channels.Email && string.IsNullOrWhiteSpace(buyer.Correo))
            throw new AppConflictException("La cuenta no tiene un correo disponible para el envío.");

        if (channels.WhatsApp && string.IsNullOrWhiteSpace(buyer.Telefono))
            throw new AppConflictException("La cuenta no tiene un teléfono disponible para WhatsApp.");

        if (channels.Email)
        {
            await emailNotifications.SendRegistrationWelcomeAsync(
                buyer.Correo,
                buyer.Nickname,
                ct);
        }

        if (channels.WhatsApp)
        {
            await whatsAppNotifications.SendRegistrationWelcomeAsync(
                buyer.Telefono!,
                buyer.Correo,
                buyer.Nickname,
                ct);
        }

        return new NotificationDispatchResult(
            request.Channel!.Trim().ToUpperInvariant(),
            "La solicitud de reenvío del mensaje de bienvenida fue procesada.");
    }
}
