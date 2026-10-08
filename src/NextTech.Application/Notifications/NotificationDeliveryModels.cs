using NextTech.Application.Common;

namespace NextTech.Application.Notifications;

public static class NotificationDeliveryChannels
{
    public const string Email = "EMAIL";
    public const string WhatsApp = "WHATSAPP";
    public const string EmailAndWhatsApp = "EMAIL_AND_WHATSAPP";

    public static (bool Email, bool WhatsApp) Parse(string? channel)
    {
        if (string.IsNullOrWhiteSpace(channel))
            throw new AppValidationException("El canal de envío es obligatorio.");

        return channel.Trim().ToUpperInvariant() switch
        {
            Email => (true, false),
            WhatsApp => (false, true),
            EmailAndWhatsApp => (true, true),
            _ => throw new AppValidationException(
                "El canal de envío debe ser EMAIL, WHATSAPP o EMAIL_AND_WHATSAPP.")
        };
    }
}

public sealed record NotificationDeliveryRequest(string? Channel);

public sealed record NotificationDispatchResult(
    string Channel,
    string Message);
