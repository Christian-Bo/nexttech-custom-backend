using System.Security.Cryptography;
using System.Text;
using NextTech.Application.Common;
using NextTech.Application.Credentials;
using NextTech.Application.Interfaces;
using NextTech.Application.Notifications;

namespace NextTech.Application.Modules.Credentials;

public sealed class BuyerCredentialService(
    ICentralIdentityGateway gateway,
    IBuyerFaceEnrollmentStore enrollmentStore,
    IBuyerCredentialCustomizationStore customizationStore,
    IBuyerCredentialPdfGenerator pdfGenerator,
    IBuyerCredentialNotificationSender notificationSender,
    IWhatsAppNotificationSender whatsAppNotifications)
{
    public Task<BuyerCredentialDocument> IssueAsync(long buyerId, CancellationToken ct)
        => IssueInternalAsync(buyerId, requestedChannels: null, ct: ct);

    public Task<BuyerCredentialDocument> ReissueAsync(
        long buyerId,
        NotificationDeliveryRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        var channels = NotificationDeliveryChannels.Parse(request.Channel);
        return IssueInternalAsync(buyerId, channels, ct);
    }

    public async Task<CredentialCustomizationContext> GetCustomizationContextAsync(
        long buyerId,
        CancellationToken ct)
    {
        var buyer = await GetActiveBuyerAsync(buyerId, ct);
        _ = await GetEnrollmentAsync(buyerId, ct);

        var current = await customizationStore.GetAsync(buyerId, ct)
            ?? BuyerCredentialDesignCatalog.Default;

        return new CredentialCustomizationContext(
            buyer.Nickname,
            "COMPRADOR",
            "/api/profile/photo",
            current,
            BuyerCredentialDesignCatalog.Templates,
            BuyerCredentialDesignCatalog.Stickers,
            BuyerCredentialDesignCatalog.StickerSlots);
    }

    public async Task<CredentialCustomization> SaveCustomizationAsync(
        long buyerId,
        CredentialCustomizationRequest request,
        CancellationToken ct)
    {
        _ = await GetActiveBuyerAsync(buyerId, ct);
        _ = await GetEnrollmentAsync(buyerId, ct);

        var customization = BuyerCredentialDesignCatalog.Normalize(request);
        await customizationStore.UpsertAsync(buyerId, customization, ct);
        return customization;
    }

    public async Task<BuyerCredentialDocument> ConfirmCustomizationAndIssueAsync(
        long buyerId,
        CredentialCustomizationRequest request,
        CancellationToken ct)
    {
        await SaveCustomizationAsync(buyerId, request, ct);
        return await IssueInternalAsync(buyerId, requestedChannels: null, ct: ct);
    }

    public async Task<BuyerCredentialDocument> PreviewAsync(
        long buyerId,
        CredentialCustomizationRequest request,
        CancellationToken ct)
    {
        var buyer = await GetActiveBuyerAsync(buyerId, ct);
        var enrollment = await GetEnrollmentAsync(buyerId, ct);
        var customization = BuyerCredentialDesignCatalog.Normalize(request);

        try
        {
            return pdfGenerator.Generate(new BuyerCredentialPdfData(
                buyer.IdUsuario,
                buyer.Nickname,
                "COMPRADOR",
                enrollment.PortraitContent,
                enrollment.PortraitContentType,
                "NEXTTECH-PREVIEW-NOT-ACTIVE",
                DateTimeOffset.UtcNow,
                customization,
                IsPreview: true));
        }
        catch (AppException)
        {
            throw;
        }
        catch (Exception)
        {
            throw new AppDependencyException("No fue posible generar la vista previa de la credencial.");
        }
    }

    private async Task<BuyerCredentialDocument> IssueInternalAsync(
        long buyerId,
        (bool Email, bool WhatsApp)? requestedChannels,
        CancellationToken ct)
    {
        var buyer = await GetActiveBuyerAsync(buyerId, ct);

        (bool Email, bool WhatsApp) channels =
            requestedChannels ?? (buyer.NotificaEmail, buyer.NotificaWhatsApp);

        if (!channels.Email && !channels.WhatsApp)
            throw new AppConflictException("La cuenta no tiene un canal de entrega habilitado.");
        if (channels.Email && string.IsNullOrWhiteSpace(buyer.Correo))
            throw new AppConflictException("La cuenta no tiene un correo disponible para la entrega.");
        if (channels.WhatsApp && string.IsNullOrWhiteSpace(buyer.Telefono))
            throw new AppConflictException("La cuenta no tiene un teléfono disponible para WhatsApp.");

        var enrollment = await GetEnrollmentAsync(buyerId, ct);
        var customization = await customizationStore.GetAsync(buyerId, ct)
            ?? BuyerCredentialDesignCatalog.Default;

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
                issuedAt,
                customization));
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

        if (channels.Email)
        {
            await notificationSender.SendCredentialAsync(
                buyer.Correo,
                buyer.Nickname,
                document,
                ct);
        }

        if (channels.WhatsApp)
        {
            await whatsAppNotifications.SendCredentialAsync(
                buyer.Telefono!,
                buyer.Nickname,
                document,
                ct);
        }

        return document;
    }

    private async Task<NextTech.Application.Authentication.BuyerAuthRecord> GetActiveBuyerAsync(
        long buyerId,
        CancellationToken ct)
    {
        var buyer = await gateway.FindByIdAsync(buyerId, ct)
            ?? throw new AppNotFoundException("Comprador no encontrado.");

        if (!buyer.Activo)
            throw new AppForbiddenException("La cuenta está inactiva.");
        if (buyer.Bloqueado)
            throw new AppForbiddenException("La cuenta está bloqueada.");

        return buyer;
    }

    private async Task<NextTech.Application.Face.BuyerFaceEnrollment> GetEnrollmentAsync(
        long buyerId,
        CancellationToken ct)
    {
        var enrollment = await enrollmentStore.GetActiveAsync(buyerId, ct)
            ?? throw new AppConflictException(
                "Debe completar el enrolamiento facial antes de trabajar con la credencial.");

        if (enrollment.PortraitContent.Length == 0)
            throw new AppConflictException(
                "El enrolamiento facial no contiene un retrato válido para la credencial.");

        return enrollment;
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
