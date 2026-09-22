using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.Email;

public sealed class SmtpNotificationSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpNotificationSender> logger) :
    IRegistrationNotificationSender,
    IRecoveryNotificationSender
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
            ct);
    }

    private async Task SendAsync(
        string recipient,
        string subject,
        string body,
        bool isBodyHtml,
        string notificationType,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SMTP deshabilitado. No se enviará el correo {NotificationType}.", notificationType);
            return;
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
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // No se registra destinatario, token, credencial QR ni contenido del mensaje.
            // Registro/recuperación mantienen una respuesta pública segura aunque SMTP falle.
            logger.LogError(ex, "No se pudo enviar el correo SMTP {NotificationType}.", notificationType);
        }
    }

    private string BuildResetUrl(string rawToken)
    {
        var separator = _options.RecoveryUrlBase.Contains("?", StringComparison.Ordinal) ? "&" : "?";
        return $"{_options.RecoveryUrlBase}{separator}token={Uri.EscapeDataString(rawToken)}";
    }
}
