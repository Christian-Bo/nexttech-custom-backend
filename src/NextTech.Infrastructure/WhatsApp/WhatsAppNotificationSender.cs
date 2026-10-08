using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextTech.Application.Credentials;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.WhatsApp;

public sealed class WhatsAppNotificationSender(
    HttpClient httpClient,
    IOptions<WhatsAppOptions> options,
    ILogger<WhatsAppNotificationSender> logger) : IWhatsAppNotificationSender
{
    private const int MaxMediaBytes = 15 * 1024 * 1024;
    private readonly WhatsAppOptions _options = options.Value;

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
            "Por seguridad, NextTech Custom nunca envía tu contraseña ni credenciales QR privadas por mensajes. " +
            "Si algún día olvidas tu contraseña, utiliza la opción de recuperación de contraseña.\n\n" +
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
            "Adjuntamos tu credencial digital de *NextTech Custom* en formato PDF. " +
            "Consérvala en un lugar seguro para utilizarla cuando la necesites.\n\n" +
            "📎 *Tu credencial incluye*\n" +
            "• Tu información de identificación.\n" +
            "• Tu fotografía registrada.\n" +
            "• Tu código QR personal de acceso.\n\n" +
            "⚠️ *Importante*\n" +
            "• Esta emisión reemplaza cualquier credencial QR anterior.\n" +
            "• No compartas el PDF, capturas ni el código QR con terceros.\n" +
            "• Si pierdes tu credencial, genera una nueva desde tu cuenta para invalidar la anterior.\n\n" +
            "🔐 *NextTech Custom — seguridad y tecnología a tu alcance.*";

        return SendAsync(
            "messages/media",
            new WhatsAppMediaRequest(
                phone,
                caption,
                document.FileName,
                document.ContentType,
                Convert.ToBase64String(document.Content)),
            "buyer-pdf-credential",
            ct);
    }

    private async Task SendAsync<TRequest>(
        string relativePath,
        TRequest payload,
        string notificationType,
        CancellationToken ct)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(relativePath, payload, ct);

            if (response.IsSuccessStatusCode)
                return;

            logger.LogWarning(
                "La API de WhatsApp rechazó la notificación {NotificationType}. StatusCode={StatusCode}.",
                notificationType,
                (int)response.StatusCode);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException ex)
        {
            logger.LogError(ex, "Timeout enviando la notificación WhatsApp {NotificationType}.", notificationType);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Error HTTP enviando la notificación WhatsApp {NotificationType}.", notificationType);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error inesperado enviando la notificación WhatsApp {NotificationType}.", notificationType);
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
