using System.Security.Cryptography;
using System.Text;
using NextTech.Application.Common;
using NextTech.Application.Credentials;
using NextTech.Application.Interfaces;

namespace NextTech.Application.Modules.Credentials;

public sealed class BuyerCredentialService(
    ICentralIdentityGateway gateway,
    IBuyerFaceEnrollmentStore enrollmentStore,
    IBuyerCredentialPdfGenerator pdfGenerator,
    IBuyerCredentialNotificationSender notificationSender)
{
    public async Task<BuyerCredentialDocument> IssueAsync(long buyerId, CancellationToken ct)
    {
        var buyer = await gateway.FindByIdAsync(buyerId, ct)
            ?? throw new AppNotFoundException("Comprador no encontrado.");

        if (!buyer.Activo)
            throw new AppForbiddenException("La cuenta está inactiva.");
        if (buyer.Bloqueado)
            throw new AppForbiddenException("La cuenta está bloqueada.");

        var enrollment = await enrollmentStore.GetActiveAsync(buyerId, ct)
            ?? throw new AppConflictException(
                "Debe completar el enrolamiento facial antes de emitir la credencial.");

        if (enrollment.PortraitContent.Length == 0)
            throw new AppConflictException(
                "El enrolamiento facial no contiene un retrato válido para la credencial.");

        var qrCredential = CreateOpaqueToken();
        var issuedAt = DateTimeOffset.UtcNow;

        BuyerCredentialDocument document;
        try
        {
            document = pdfGenerator.Generate(new BuyerCredentialPdfData(
                buyer.IdUsuario,
                buyer.Nickname,
                "COMPRADOR",
                enrollment.PortraitContent,
                enrollment.PortraitContentType,
                qrCredential,
                issuedAt));
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new AppDependencyException("No fue posible generar la credencial PDF.");
        }

        try
        {
            await gateway.SetQrHashAsync(buyer.IdUsuario, HashToken(qrCredential), ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            throw new AppDependencyException("No fue posible activar la nueva credencial QR.");
        }

        await notificationSender.SendCredentialAsync(
            buyer.Correo,
            buyer.Nickname,
            document,
            ct);

        return document;
    }

    private static string CreateOpaqueToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

    private static string HashToken(string raw)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)))
            .ToLowerInvariant();
}
