using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextTech.Application.Credentials;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.Email;

public sealed class SmtpNotificationSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpNotificationSender> logger) :
    IRegistrationNotificationSender,
    IRecoveryNotificationSender,
    IBuyerCredentialNotificationSender,
    IOrderMailSender
{
    private readonly SmtpOptions _options = options.Value;

    public Task SendRegistrationCredentialAsync(
        string email,
        string nickname,
        string qrCredential,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(qrCredential))
            throw new ArgumentException("La credencial QR no puede estar vacía.", nameof(qrCredential));

        var safeNickname = WebUtility.HtmlEncode(nickname);
        var safeCredential = WebUtility.HtmlEncode(qrCredential);

        var body = $$"""
            <!doctype html>
            <html lang="es">
            <body style="font-family:Arial,sans-serif;color:#2D3035;line-height:1.5">
              <h2 style="margin-bottom:8px">Bienvenido a NextTech Custom</h2>
              <p>Hola <strong>{{safeNickname}}</strong>, tu registro se completó correctamente.</p>
              <p>Esta es tu credencial QR de acceso. El frontend la utilizará para representar tu código QR.</p>
              <div style="padding:14px;border:1px solid #d8d8d8;border-radius:8px;background:#f5f3ef;word-break:break-all;font-family:Consolas,monospace">
                {{safeCredential}}
              </div>
              <p><strong>No compartas esta credencial.</strong> Permite iniciar sesión en tu cuenta mediante QR.</p>
              <p>NextTech Solution</p>
            </body>
            </html>
            """;

        return SendAsync(
            email,
            "NextTech Custom - Credencial de registro",
            body,
            isBodyHtml: true,
            notificationType: "registration-credential",
            attachment: null,
            ct);
    }

    public Task SendPasswordRecoveryAsync(
        string email,
        string rawToken,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        var resetUrl = BuildResetUrl(rawToken);
        var safeUrl = WebUtility.HtmlEncode(resetUrl);
        var safeExpiration = WebUtility.HtmlEncode(expiresAt.ToUniversalTime().ToString("u"));

        var body = $$"""
            <!doctype html>
            <html lang="es">
            <body style="font-family:Arial,sans-serif;color:#2D3035;line-height:1.5">
              <h2 style="margin-bottom:8px">Recuperación de contraseña</h2>
              <p>Se solicitó restablecer tu contraseña de NextTech Custom.</p>
              <p>
                <a href="{{safeUrl}}" style="display:inline-block;padding:10px 16px;background:#7B8C7A;color:white;text-decoration:none;border-radius:6px">
                  Restablecer contraseña
                </a>
              </p>
              <p>El enlace vence a las {{safeExpiration}} UTC.</p>
              <p>Si no realizaste esta solicitud, ignora este mensaje.</p>
              <p>NextTech Solution</p>
            </body>
            </html>
            """;

        return SendAsync(
            email,
            "NextTech Custom - Recuperación de contraseña",
            body,
            isBodyHtml: true,
            notificationType: "password-recovery",
            attachment: null,
            ct);
    }

    public Task SendCredentialAsync(
        string email,
        string nickname,
        BuyerCredentialDocument document,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.Content.Length == 0)
            throw new ArgumentException("El PDF de la credencial no puede estar vacío.", nameof(document));

        var safeNickname = WebUtility.HtmlEncode(nickname);
        var body = $$"""
            <!doctype html>
            <html lang="es">
            <body style="font-family:Arial,sans-serif;color:#2D3035;line-height:1.5">
              <h2 style="margin-bottom:8px">Tu credencial NextTech Custom</h2>
              <p>Hola <strong>{{safeNickname}}</strong>.</p>
              <p>Adjuntamos tu credencial digital en formato PDF con tu fotografía y código QR de acceso.</p>
              <p><strong>Importante:</strong> la emisión de esta credencial reemplaza cualquier QR anterior.</p>
              <p>No compartas el PDF ni el código QR con terceros.</p>
              <p>NextTech Solution</p>
            </body>
            </html>
            """;

        return SendAsync(
            email,
            "NextTech Custom - Credencial digital",
            body,
            isBodyHtml: true,
            notificationType: "buyer-pdf-credential",
            new EmailAttachment(document.FileName, document.ContentType, document.Content),
            ct);
    }

    public Task<bool> SendPurchaseConfirmationAsync(
        string email,
        string nickname,
        string codigoOrden,
        byte[] pdf,
        CancellationToken cancellationToken)
    {
        var safeNickname = WebUtility.HtmlEncode(nickname);
        var safeCodigo = WebUtility.HtmlEncode(codigoOrden);
        var body = $$"""
            <!doctype html>
            <html lang="es">
            <body style="font-family:Arial,sans-serif;color:#2D3035;line-height:1.5">
              <h2 style="margin-bottom:8px">Confirmación de compra</h2>
              <p>Hola <strong>{{safeNickname}}</strong>.</p>
              <p>Tu orden <strong>{{safeCodigo}}</strong> fue generada. Adjuntamos la constancia PDF con el código QR de entrega.</p>
              <p>El pago en efectivo se confirma al recibir el producto.</p>
              <p>NextTech Solution</p>
            </body>
            </html>
            """;

        EmailAttachment? attachment = pdf.Length == 0
            ? null
            : new EmailAttachment($"constancia-{codigoOrden}.pdf", "application/pdf", pdf);

        return SendAsync(
            email,
            $"NextTech Custom - Constancia {codigoOrden}",
            body,
            isBodyHtml: true,
            notificationType: "order-confirmation",
            attachment,
            cancellationToken);
    }

    public Task<bool> SendOrderReadyAsync(
        string email,
        string nickname,
        string codigoOrden,
        CancellationToken cancellationToken)
    {
        var safeNickname = WebUtility.HtmlEncode(nickname);
        var safeCodigo = WebUtility.HtmlEncode(codigoOrden);
        var body = $$"""
            <!doctype html>
            <html lang="es">
            <body style="font-family:Arial,sans-serif;color:#2D3035;line-height:1.5">
              <h2 style="margin-bottom:8px">Pedido listo para entrega</h2>
              <p>Hola <strong>{{safeNickname}}</strong>.</p>
              <p>Tu orden <strong>{{safeCodigo}}</strong> ya está lista. Un repartidor la tomará para entregarla en el área que elegiste.</p>
              <p>NextTech Solution</p>
            </body>
            </html>
            """;

        return SendAsync(
            email,
            $"NextTech Custom - Pedido listo {codigoOrden}",
            body,
            isBodyHtml: true,
            notificationType: "order-ready",
            attachment: null,
            cancellationToken);
    }

    public Task<bool> SendDeliveryConfirmedAsync(
        string email,
        string nickname,
        string codigoOrden,
        CancellationToken cancellationToken)
    {
        var safeNickname = WebUtility.HtmlEncode(nickname);
        var safeCodigo = WebUtility.HtmlEncode(codigoOrden);
        var body = $$"""
            <!doctype html>
            <html lang="es">
            <body style="font-family:Arial,sans-serif;color:#2D3035;line-height:1.5">
              <h2 style="margin-bottom:8px">Entrega confirmada</h2>
              <p>Hola <strong>{{safeNickname}}</strong>.</p>
              <p>Tu orden <strong>{{safeCodigo}}</strong> fue entregada. Gracias por comprar en NextTech Custom.</p>
              <p>NextTech Solution</p>
            </body>
            </html>
            """;

        return SendAsync(
            email,
            $"NextTech Custom - Entrega {codigoOrden}",
            body,
            isBodyHtml: true,
            notificationType: "order-delivered",
            attachment: null,
            cancellationToken);
    }

    private async Task<bool> SendAsync(
        string recipient,
        string subject,
        string body,
        bool isBodyHtml,
        string notificationType,
        EmailAttachment? attachment,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SMTP deshabilitado. No se enviará el correo {NotificationType}.", notificationType);
            return false;
        }

        ct.ThrowIfCancellationRequested();

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From, "NextTech Solution"),
            Subject = subject,
            Body = body,
            IsBodyHtml = isBodyHtml
        };
        message.To.Add(new MailAddress(recipient));

        if (attachment is not null)
        {
            var stream = new MemoryStream(attachment.Content, writable: false);
            message.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
        }

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.UserName, _options.Password),
            DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = checked(_options.TimeoutSeconds * 1000)
        };

        try
        {
            await client.SendMailAsync(message).WaitAsync(ct);
            return true;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // No se registra destinatario, token, QR, PDF ni contenido del mensaje.
            // La operación principal no falla si el proveedor SMTP no está disponible.
            logger.LogError(ex, "No se pudo enviar el correo SMTP {NotificationType}.", notificationType);
            return false;
        }
    }

    private string BuildResetUrl(string rawToken)
    {
        var separator = _options.RecoveryUrlBase.Contains("?", StringComparison.Ordinal) ? "&" : "?";
        return $"{_options.RecoveryUrlBase}{separator}token={Uri.EscapeDataString(rawToken)}";
    }

    private sealed record EmailAttachment(
        string FileName,
        string ContentType,
        byte[] Content);
}
