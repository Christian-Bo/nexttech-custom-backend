using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;

namespace NextTech.Application.Modules.Face;

public sealed class FaceApplicationService(
    ICentralIdentityGateway gateway,
    IFaceBiometricService biometrics,
    IBuyerFaceEnrollmentStore enrollmentStore)
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

        // The provider is stateless for NextTech buyers. The protected template is used
        // only for this verification and is intentionally not persisted because the
        // shared Oracle schema stores the buyer's original and processed photographs.
        var enrollment = await biometrics.CreateTemplateAsync(neutralImage, ct);
        var verification = await biometrics.VerifyTemplateLiveAsync(
            enrollment.BiometricTemplate.Value,
            challengeId.Trim(),
            neutralImage,
            challengeImage,
            ct);

        EnsureAuthenticated(verification, "No se superó la validación de presencia activa e identidad facial.");

        var now = DateTimeOffset.UtcNow;
        await enrollmentStore.UpsertAsync(
            new BuyerFaceEnrollment(
                buyer.IdUsuario,
                neutralImage,
                enrollment.Portrait.Content,
                enrollment.Portrait.ContentType,
                now),
            ct);

        return new FaceEnrollmentResult(
            true,
            enrollment.BiometricTemplate.Version,
            enrollment.BiometricTemplate.Model,
            enrollment.Portrait.Width,
            enrollment.Portrait.Height,
            now,
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
        var enrollment = await enrollmentStore.GetActiveAsync(buyer.IdUsuario, ct)
            ?? throw new AppConflictException("El comprador todavía no tiene enrolamiento facial activo.");

        var transientTemplate = await biometrics.CreateTemplateAsync(enrollment.ReferenceImage, ct);

        return await biometrics.VerifyTemplateLiveAsync(
            transientTemplate.BiometricTemplate.Value,
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
