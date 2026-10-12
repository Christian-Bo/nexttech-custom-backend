using System.Globalization;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextTech.Application.Credentials;
using NextTech.Application.Interfaces;
using NextTech.Infrastructure.Email;

namespace NextTech.Infrastructure.WhatsApp;

public sealed class WhatsAppNotificationSender(
    HttpClient httpClient,
    IOptions<WhatsAppOptions> options,
    IOptions<SmtpOptions> smtpOptions,
    ILogger<WhatsAppNotificationSender> logger) : IWhatsAppNotificationSender
{
    private const int MaxMediaBytes = 15 * 1024 * 1024;
    private readonly WhatsAppOptions _options = options.Value;
    private readonly SmtpOptions _smtpOptions = smtpOptions.Value;

    public Task SendRegistrationWelcomeAsync(
        string phone,
        string email,
        string nickname,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("WhatsApp deshabilitado. No se enviará el mensaje de bienvenida.");
            return Task.CompletedTask;
        }

        var message =
            "🎉 *¡Bienvenido a NextTech Custom!*\n\n" +
            $"Hola *{nickname}* 👋\n" +
            "Tu cuenta fue creada correctamente y ya puedes comenzar a utilizar nuestros servicios.\n\n" +
            "👤 *Datos de tu cuenta*\n" +
            $"📧 Correo: {email}\n" +
            $"🏷️ Usuario: {nickname}\n" +
            "🔐 Contraseña: configurada de forma segura\n\n" +
            "🛡️ *Tu seguridad es importante*\n" +
            "NextTech Custom nunca enviará tu contraseña ni el valor interno de tu credencial QR por mensajes. " +
            "Si olvidas tu contraseña, utiliza la opción de recuperación de acceso.\n\n" +
            "✨ Gracias por formar parte de *NextTech Custom*.";

        return SendAsync(
            "messages/text",
            new WhatsAppTextRequest(phone, message),
            "registration-welcome",
            ct);
    }

    public Task SendCredentialAsync(
        string phone,
        string nickname,
        BuyerCredentialDocument document,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Content.Length == 0)
            throw new ArgumentException("El PDF de la credencial no puede estar vacío.", nameof(document));

        if (!_options.Enabled)
        {
            logger.LogInformation("WhatsApp deshabilitado. No se enviará la credencial PDF.");
            return Task.CompletedTask;
        }

        if (document.Content.Length > MaxMediaBytes)
        {
            logger.LogError(
                "La credencial PDF supera el límite de {MaxMediaBytes} bytes admitido por la integración de WhatsApp.",
                MaxMediaBytes);
            return Task.CompletedTask;
        }

        var caption =
            "🪪 *Tu credencial digital está lista*\n\n" +
            $"Hola *{nickname}* 👋\n\n" +
            "Adjuntamos tu credencial digital personalizada de *NextTech Custom* en formato PDF.\n\n" +
            "📎 *Tu credencial incluye*\n" +
            "• Tu diseño personalizado.\n" +
            "• Tu identificación como comprador.\n" +
            "• Tu código QR personal de acceso.\n\n" +
            "⚠️ *Importante*\n" +
            "• Esta emisión reemplaza cualquier credencial QR anterior.\n" +
            "• No compartas el PDF, capturas ni el código QR con terceros.\n" +
            "• Si pierdes tu credencial, genera una nueva para invalidar la anterior.\n\n" +
            "🔐 *NextTech Custom — seguridad y tecnología a tu alcance.*";

        return SendMediaAsync(
            phone,
            caption,
            document.FileName,
            document.ContentType,
            document.Content,
            "buyer-pdf-credential",
            ct);
    }

    public Task SendPasswordRecoveryAsync(
        string phone,
        string nickname,
        string rawToken,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(_smtpOptions.RecoveryUrlBase))
        {
            logger.LogWarning("No se envió recuperación por WhatsApp porque Smtp:RecoveryUrlBase no está configurado.");
            return Task.CompletedTask;
        }

        var separator = _smtpOptions.RecoveryUrlBase.Contains("?", StringComparison.Ordinal) ? '&' : '?';
        var url = $"{_smtpOptions.RecoveryUrlBase}{separator}token={Uri.EscapeDataString(rawToken)}";
        var message =
            "🔐 *Recuperación de acceso — NextTech Custom*\n\n" +
            $"Hola *{nickname}* 👋\n" +
            "Recibimos una solicitud para restablecer la contraseña de tu cuenta.\n\n" +
            $"🔗 {url}\n\n" +
            $"⏳ El enlace vence el {expiresAt.ToLocalTime():dd/MM/yyyy 'a las' HH:mm}.\n\n" +
            "⚠️ Si tú no solicitaste este cambio, ignora el mensaje y no compartas el enlace con nadie.";

        return SendAsync(
            "messages/text",
            new WhatsAppTextRequest(phone, message),
            "password-recovery",
            ct);
    }

    public Task SendPasswordChangedAsync(
        string phone,
        string nickname,
        CancellationToken ct)
    {
        if (!_options.Enabled)
            return Task.CompletedTask;

        var message =
            "🔐 *Tu contraseña fue actualizada*\n\n" +
            $"Hola *{nickname}* 👋\n" +
            "La contraseña de tu cuenta NextTech Custom fue modificada correctamente.\n\n" +
            $"📅 Fecha: {DateTimeOffset.Now:dd/MM/yyyy}\n" +
            $"🕒 Hora: {DateTimeOffset.Now:HH:mm}\n\n" +
            "⚠️ Si tú no realizaste este cambio, comunícate inmediatamente con soporte y protege tu cuenta.";

        return SendAsync(
            "messages/text",
            new WhatsAppTextRequest(phone, message),
            "password-changed",
            ct);
    }

    public Task<bool> SendPurchaseConfirmationAsync(
        string phone,
        string nickname,
        string orderCode,
        decimal total,
        string paymentStatus,
        byte[] receiptPdf,
        CancellationToken ct)
    {
        var caption =
            "🛍️ *¡Compra confirmada!*\n\n" +
            $"Hola *{nickname}* 👋\n" +
            "Hemos recibido correctamente tu pedido.\n\n" +
            $"🧾 Pedido: *{orderCode}*\n" +
            $"💰 Total: *Q{total.ToString("N2", CultureInfo.InvariantCulture)}*\n" +
            $"💳 Pago: *{paymentStatus}*\n" +
            "📦 Estado: *En elaboración*\n\n" +
            "Adjuntamos tu constancia de compra en PDF. Te avisaremos cuando el pedido avance.\n\n" +
            "✨ Gracias por comprar en *NextTech Custom*.";

        return SendMediaAsync(
            phone,
            caption,
            $"constancia-{orderCode}.pdf",
            "application/pdf",
            receiptPdf,
            "order-confirmation",
            ct);
    }

    public Task<bool> SendOrderReadyAsync(
        string phone,
        string nickname,
        string orderCode,
        CancellationToken ct)
    {
        var message =
            "📦 *¡Tu pedido está listo!*\n\n" +
            $"Hola *{nickname}* 👋\n" +
            $"Tu pedido *{orderCode}* terminó su preparación y está listo para ser asignado a un repartidor.\n\n" +
            "Te notificaremos nuevamente cuando salga para entrega.";

        return SendTextAsync(phone, message, "order-ready", ct);
    }

    public Task<bool> SendOrderOutForDeliveryAsync(
        string phone,
        string nickname,
        string orderCode,
        CancellationToken ct)
    {
        var message =
            "🚚 *¡Tu pedido está en camino!*\n\n" +
            $"Hola *{nickname}* 👋\n" +
            $"Tu pedido *{orderCode}* ya fue tomado por un repartidor y se encuentra en ruta de entrega.\n\n" +
            "📱 Mantente pendiente del teléfono registrado por si necesitamos contactarte.\n\n" +
            "🔐 Por tu seguridad, nunca compartas tu contraseña ni credenciales privadas con el repartidor.";

        return SendTextAsync(phone, message, "order-out-for-delivery", ct);
    }

    public Task<bool> SendDeliveryConfirmedAsync(
        string phone,
        string nickname,
        string orderCode,
        CancellationToken ct)
    {
        var message =
            "✅ *Pedido entregado*\n\n" +
            $"Hola *{nickname}* 👋\n" +
            $"Tu pedido *{orderCode}* fue marcado como entregado correctamente.\n\n" +
            "🎉 Esperamos que disfrutes tu compra. Gracias por confiar en *NextTech Custom*.";

        return SendTextAsync(phone, message, "order-delivered", ct);
    }

    public Task<bool> SendDeliveryIssueAsync(
        string phone,
        string nickname,
        string orderCode,
        string reason,
        CancellationToken ct)
    {
        var message =
            "⚠️ *No pudimos completar la entrega*\n\n" +
            $"Hola *{nickname}* 👋\n" +
            $"La entrega del pedido *{orderCode}* no pudo completarse en este intento.\n\n" +
            $"ℹ️ Motivo: {reason}\n\n" +
            "El pedido volverá a quedar disponible para coordinar un nuevo intento. No compartas información sensible por mensajes.";

        return SendTextAsync(phone, message, "delivery-issue", ct);
    }

    private Task<bool> SendTextAsync(
        string phone,
        string message,
        string notificationType,
        CancellationToken ct)
    {
        if (!_options.Enabled)
            return Task.FromResult(false);

        return SendAsync(
            "messages/text",
            new WhatsAppTextRequest(phone, message),
            notificationType,
            ct);
    }

    private Task<bool> SendMediaAsync(
        string phone,
        string caption,
        string fileName,
        string contentType,
        byte[] content,
        string notificationType,
        CancellationToken ct)
    {
        if (!_options.Enabled)
            return Task.FromResult(false);

        if (content.Length == 0 || content.Length > MaxMediaBytes)
        {
            logger.LogWarning(
                "No se envió {NotificationType}: tamaño de archivo inválido ({Size} bytes).",
                notificationType,
                content.Length);
            return Task.FromResult(false);
        }

        return SendAsync(
            "messages/media",
            new WhatsAppMediaRequest(
                phone,
                caption,
                fileName,
                contentType,
                Convert.ToBase64String(content)),
            notificationType,
            ct);
    }

    private async Task<bool> SendAsync<TRequest>(
        string relativePath,
        TRequest payload,
        string notificationType,
        CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(relativePath, payload, ct);

            if (response.IsSuccessStatusCode)
                return true;

            logger.LogWarning(
                "La API de WhatsApp rechazó la notificación {NotificationType}. StatusCode={StatusCode}.",
                notificationType,
                (int)response.StatusCode);
            return false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Timeout enviando la notificación WhatsApp {NotificationType}.", notificationType);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error HTTP enviando la notificación WhatsApp {NotificationType}.", notificationType);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado enviando la notificación WhatsApp {NotificationType}.", notificationType);
            return false;
        }
    }

    private sealed record WhatsAppTextRequest(string To, string Message);

    private sealed record WhatsAppMediaRequest(
        string To,
        string Caption,
        string Filename,
        string MimeType,
        string Base64Data);
}
