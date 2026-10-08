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

    public Task SendRegistrationWelcomeAsync(
        string email,
        string nickname,
        CancellationToken ct)
    {
        var safeEmail = WebUtility.HtmlEncode(email);
        var safeNickname = WebUtility.HtmlEncode(nickname);

        var body = $$"""
            <!doctype html>
            <html lang="es">
            <body style="margin:0;padding:0;background:#f4f6f8;font-family:Arial,sans-serif;color:#25313c">
              <div style="max-width:640px;margin:0 auto;padding:28px 16px">
                <div style="background:#ffffff;border-radius:14px;padding:30px;box-shadow:0 4px 18px rgba(0,0,0,.08)">
                  <div style="font-size:34px;margin-bottom:8px">🎉</div>
                  <h2 style="margin:0 0 10px;color:#18232d">¡Bienvenido a NextTech Custom!</h2>
                  <p style="margin:0 0 22px">Hola <strong>{{safeNickname}}</strong> 👋. Tu cuenta fue creada correctamente y ya puedes comenzar a utilizar nuestros servicios.</p>

                  <div style="background:#f7f9fb;border:1px solid #e1e7ec;border-radius:10px;padding:18px;margin-bottom:22px">
                    <h3 style="margin:0 0 12px;font-size:17px">👤 Datos de tu cuenta</h3>
                    <p style="margin:6px 0"><strong>📧 Correo:</strong> {{safeEmail}}</p>
                    <p style="margin:6px 0"><strong>🏷️ Usuario:</strong> {{safeNickname}}</p>
                    <p style="margin:6px 0"><strong>🔐 Contraseña:</strong> configurada de forma segura</p>
                  </div>

                  <div style="border-left:4px solid #d89b2b;background:#fff8e8;padding:14px 16px;border-radius:6px">
                    <strong>🛡️ Tu seguridad es importante</strong>
                    <p style="margin:8px 0 0">NextTech Custom nunca envía tu contraseña ni credenciales QR privadas por correo. Si olvidas tu contraseña, utiliza la opción de recuperación de contraseña.</p>
                  </div>

                  <p style="margin:24px 0 0">✨ Gracias por formar parte de <strong>NextTech Custom</strong>.</p>
                  <p style="margin:8px 0 0;color:#68737d">NextTech Solution</p>
                </div>
              </div>
            </body>
            </html>
            """;

        return SendAsync(
            email,
            "Bienvenido a NextTech Custom",
            body,
            isBodyHtml: true,
            notificationType: "registration-welcome",
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
            <body style="margin:0;padding:0;background:#f4f6f8;font-family:Arial,sans-serif;color:#25313c">
              <div style="max-width:640px;margin:0 auto;padding:28px 16px">
                <div style="background:#ffffff;border-radius:14px;padding:30px;box-shadow:0 4px 18px rgba(0,0,0,.08)">
                  <div style="font-size:34px;margin-bottom:8px">🪪</div>
                  <h2 style="margin:0 0 10px;color:#18232d">Tu credencial digital está lista</h2>
                  <p>Hola <strong>{{safeNickname}}</strong> 👋.</p>
                  <p>Adjuntamos tu credencial digital de <strong>NextTech Custom</strong> en formato PDF. Consérvala en un lugar seguro para utilizarla cuando la necesites.</p>

                  <div style="background:#f7f9fb;border:1px solid #e1e7ec;border-radius:10px;padding:18px;margin:20px 0">
                    <strong>📎 Tu credencial incluye</strong>
                    <ul style="padding-left:22px;margin-bottom:0">
                      <li>Tu información de identificación.</li>
                      <li>Tu fotografía registrada.</li>
                      <li>Tu código QR personal de acceso.</li>
                    </ul>
                  </div>

                  <div style="border-left:4px solid #c85050;background:#fff1f1;padding:14px 16px;border-radius:6px">
                    <strong>⚠️ Importante</strong>
                    <ul style="padding-left:22px;margin:8px 0 0">
                      <li>Esta emisión reemplaza cualquier credencial QR anterior.</li>
                      <li>No compartas el PDF, capturas ni el código QR con terceros.</li>
                      <li>Si pierdes tu credencial, genera una nueva desde tu cuenta para invalidar la anterior.</li>
                    </ul>
                  </div>

                  <p style="margin:24px 0 0">🔐 <strong>NextTech Custom — seguridad y tecnología a tu alcance.</strong></p>
                  <p style="margin:8px 0 0;color:#68737d">NextTech Solution</p>
                </div>
              </div>
            </body>
            </html>
            """;

        return SendAsync(
            email,
            "NextTech Custom - Tu credencial digital está lista",
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
