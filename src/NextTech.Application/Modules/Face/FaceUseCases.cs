using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;

namespace NextTech.Application.Modules.Face;

public sealed class FaceApplicationService(
    ICentralIdentityGateway gateway,
    IFaceBiometricService biometrics,
    IBuyerBiometricStore biometricStore)
{
    public async Task<FaceChallengeResult> CreateChallengeAsync(long buyerId, CancellationToken ct)
    {
        await GetActiveBuyerAsync(buyerId, ct);
        return await biometrics.CreateLivenessChallengeAsync(ct);
    }

    public Task<FaceChallengeResult> CreateAnonymousChallengeAsync(CancellationToken ct)
        => biometrics.CreateLivenessChallengeAsync(ct);

    public async Task<FaceEnrollmentResult> EnrollBuyerAsync(
        long buyerId,
        string challengeId,
        FaceImage neutralImage,
        FaceImage challengeImage,
        CancellationToken ct)
    {
        ValidateChallengeRequest(challengeId, neutralImage, challengeImage);
        var buyer = await GetActiveBuyerAsync(buyerId, ct);

        // The provider is stateless with respect to NextTech buyers. Create the protected
        // template in memory, verify liveness + identity, and persist only after success.
        var enrollment = await biometrics.CreateTemplateAsync(neutralImage, ct);
        var verification = await biometrics.VerifyTemplateLiveAsync(
            enrollment.BiometricTemplate.Value,
            challengeId.Trim(),
            neutralImage,
            challengeImage,
            ct);

        EnsureAuthenticated(verification, "No se superó la validación de presencia activa e identidad facial.");

        var now = DateTimeOffset.UtcNow;
        var stored = await biometricStore.UpsertAsync(new BuyerBiometricCredential(
            buyer.IdUsuario,
            enrollment.BiometricTemplate.Value,
            enrollment.BiometricTemplate.Version,
            enrollment.BiometricTemplate.KeyId,
            enrollment.BiometricTemplate.Model,
            enrollment.BiometricTemplate.Dimensions,
            enrollment.BiometricTemplate.EmbeddingSha256,
            enrollment.Portrait.Content,
            enrollment.Portrait.ContentType,
            enrollment.Portrait.Width,
            enrollment.Portrait.Height,
            enrollment.Portrait.Background,
            now,
            null), ct);

        return new FaceEnrollmentResult(
            true,
            stored.TemplateVersion,
            stored.TemplateModel,
            stored.PortraitWidth,
            stored.PortraitHeight,
            stored.EnrolledAtUtc,
            enrollment.Message);
    }

    public async Task<FaceVerificationResult> VerifyBuyerAsync(
        long buyerId,
        string challengeId,
        FaceImage neutralImage,
        FaceImage challengeImage,
        CancellationToken ct)
    {
        ValidateChallengeRequest(challengeId, neutralImage, challengeImage);
        var buyer = await GetActiveBuyerAsync(buyerId, ct);
        var credential = await biometricStore.GetActiveAsync(buyer.IdUsuario, ct)
            ?? throw new AppConflictException("El comprador todavía no tiene enrolamiento facial activo.");

        return await biometrics.VerifyTemplateLiveAsync(
            credential.BiometricTemplate,
            challengeId.Trim(),
            neutralImage,
            challengeImage,
            ct);
    }

    public async Task<FaceSegmentationResult> SegmentForCardAsync(
        long buyerId,
        FaceImage image,
        CancellationToken ct)
    {
        FaceImageValidator.Validate(image);
        await GetActiveBuyerAsync(buyerId, ct);
        return await biometrics.SegmentForCardAsync(image, ct);
    }

    private static void ValidateChallengeRequest(
        string challengeId,
        FaceImage neutralImage,
        FaceImage challengeImage)
    {
        if (string.IsNullOrWhiteSpace(challengeId))
            throw new AppValidationException("El challengeId es obligatorio.");

        if (challengeId.Length > 200)
            throw new AppValidationException("El challengeId no es válido.");

        FaceImageValidator.Validate(neutralImage);
        FaceImageValidator.Validate(challengeImage);
    }

    private static void EnsureAuthenticated(FaceVerificationResult result, string message)
    {
        if (!result.AuthenticationPassed || !result.IsLive || result.IsMatch != true)
            throw new AppUnprocessableException(message);
    }

    private async Task<BuyerAuthRecord> GetActiveBuyerAsync(long buyerId, CancellationToken ct)
    {
        var buyer = await gateway.FindByIdAsync(buyerId, ct)
            ?? throw new AppNotFoundException("Comprador no encontrado.");

        if (!buyer.Activo)
            throw new AppForbiddenException("La cuenta está inactiva.");
        if (buyer.Bloqueado)
            throw new AppForbiddenException("La cuenta está bloqueada.");

        return buyer;
    }
}
