using NextTech.Application.Face;
using NextTech.Application.Modules.Face;

namespace NextTech.Application.Modules.Onboarding;

/// <summary>
/// Completes the biometric phase of buyer onboarding.
/// The neutral image is the persistent biometric reference; challengeImage is transient
/// and used only for liveness. Credential customization and issuance are separate so
/// the frontend can finish the visual design before the official PDF is generated.
/// </summary>
public sealed class BuyerOnboardingService(FaceApplicationService face)
{
    public Task<FaceEnrollmentResult> CompleteAsync(
        long buyerId,
        string challengeId,
        FaceImage neutralImage,
        FaceImage challengeImage,
        CancellationToken ct)
        => face.EnrollBuyerAsync(
            buyerId,
            challengeId,
            neutralImage,
            challengeImage,
            ct);
}
