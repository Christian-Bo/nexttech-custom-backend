using NextTech.Application.Face;

namespace NextTech.Application.Interfaces;

public interface IFaceBiometricService
{
    bool IdentificationEnabled { get; }

    Task<FaceChallengeResult> CreateLivenessChallengeAsync(CancellationToken ct);

    Task<ProtectedFaceEnrollmentResult> CreateTemplateAsync(
        FaceImage image,
        CancellationToken ct);

    Task<FaceVerificationResult> VerifyTemplateLiveAsync(
        string biometricTemplate,
        string challengeId,
        FaceImage neutralImage,
        FaceImage challengeImage,
        CancellationToken ct);

    Task RegisterIdentityAsync(
        long buyerId,
        FaceImage referenceImage,
        CancellationToken ct);

    Task<FaceQuickIdentificationResult> IdentifyAsync(
        FaceImage image,
        CancellationToken ct);

    Task<FaceIdentificationResult> IdentifyLiveAsync(
        string challengeId,
        FaceImage neutralImage,
        FaceImage challengeImage,
        CancellationToken ct);

    Task<FaceSegmentationResult> SegmentForCardAsync(
        FaceImage image,
        CancellationToken ct);
}
