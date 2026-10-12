using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.Email;

public sealed class SmtpRecoveryNotificationSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpRecoveryNotificationSender> logger) : IRecoveryNotificationSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendPasswordRecoveryAsync(
        string email,
        string rawToken,
        DateTimeOffset expiresAt,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SMTP deshabilitado. No se enviara correo de recuperacion.");
            return;
        }

        ct.ThrowIfCancellationRequested();

        var resetUrl = BuildResetUrl(rawToken);

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = "NextTech Custom - Recuperacion de contrasena",
            Body = $"Se solicito restablecer tu contrasena de NextTech Custom.\n\n" +
                   $"Abre este enlace para continuar:\n{resetUrl}\n\n" +
                   $"El enlace vence a las {expiresAt:O}.\n" +
                   "Si no realizaste esta solicitud, ignora este mensaje.",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(email));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.UserName, _options.Password)
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // La respuesta publica de recuperacion debe seguir siendo neutra para evitar enumeracion de cuentas.
            // El fallo queda registrado para operacion y el usuario puede volver a solicitar recuperacion.
            logger.LogError(ex, "No se pudo enviar el correo de recuperacion por SMTP.");
        }
    }

    public async Task SendPasswordChangedAsync(
        string email,
        string nickname,
        CancellationToken ct)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("SMTP deshabilitado. No se enviara alerta de cambio de contrasena.");
            return;
        }

        ct.ThrowIfCancellationRequested();

        using var message = new MailMessage
        {
            From = new MailAddress(_options.From),
            Subject = "NextTech Custom - Tu contrasena fue actualizada",
            Body = $"Hola {nickname},\n\n" +
                   "La contrasena de tu cuenta NextTech Custom fue modificada correctamente.\n\n" +
                   "Si tu realizaste este cambio, no necesitas hacer nada. " +
                   "Si no reconoces esta actividad, contacta inmediatamente con soporte y protege tu cuenta.",
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(email));

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_options.UserName, _options.Password)
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "No se pudo enviar la alerta de cambio de contrasena por SMTP.");
        }
    }

    private string BuildResetUrl(string rawToken)
    {
        var separator = _options.RecoveryUrlBase.Contains("?", StringComparison.Ordinal) ? "&" : "?";
        return $"{_options.RecoveryUrlBase}{separator}token={Uri.EscapeDataString(rawToken)}";
    }
}
