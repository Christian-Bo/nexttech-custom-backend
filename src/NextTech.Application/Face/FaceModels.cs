namespace NextTech.Application.Face;

public sealed record FaceImage(
    byte[] Content,
    string ContentType,
    string FileName);

public sealed record FaceChallengeResult(
    string Id,
    string Action,
    string Instruction,
    DateTimeOffset ExpiresAtUtc,
    int ExpiresInSeconds);

public sealed record ProtectedBiometricTemplate(
    string Value,
    string Version,
    string KeyId,
    string Model,
    int Dimensions,
    string? EmbeddingSha256);

public sealed record ProcessedFacePortrait(
    byte[] Content,
    string ContentType,
    int Width,
    int Height,
    string? Background);

public sealed record ProtectedFaceEnrollmentResult(
    ProtectedBiometricTemplate BiometricTemplate,
    ProcessedFacePortrait Portrait,
    decimal? FaceScore,
    string? QualityStatus,
    int WarningCount,
    string? Message);

public sealed record BuyerFaceEnrollment(
    long BuyerId,
    FaceImage ReferenceImage,
    byte[] PortraitContent,
    string PortraitContentType,
    DateTimeOffset EnrolledAtUtc);

public sealed record FaceEnrollmentResult(
    bool Enrolled,
    string TemplateVersion,
    string Model,
    int PortraitWidth,
    int PortraitHeight,
    DateTimeOffset EnrolledAtUtc,
    string? Message = null);

public sealed record FaceVerificationResult(
    bool AuthenticationPassed,
    string Decision,
    bool IsLive,
    string LivenessDecision,
    string? LivenessReasonCode,
    string Action,
    decimal? YawDelta,
    decimal? RequiredYawDelta,
    decimal? FrameIdentitySimilarity,
    decimal? FrameIdentityThreshold,
    bool? IsMatch,
    decimal? Similarity,
    decimal? Threshold,
    decimal? DecisionMargin,
    string? Message = null);


public sealed record FaceIdentificationResult(
    bool Identified,
    long? BuyerId,
    bool AuthenticationPassed,
    bool IsLive,
    decimal? Similarity,
    decimal? Threshold,
    string? Message = null);

// Internal-only provider diagnostics. Never serialize this as a public HTTP response.
public sealed record FaceQuickIdentificationResult(
    bool Identified,
    string? SubjectId,
    decimal? Similarity,
    decimal? Threshold,
    double? FaceApiProcessingMs,
    double? FaceApiSearchMs,
    double? FaceApiEmbeddingMs,
    double FaceApiHttpMs,
    string? FaceApiTraceId);

public sealed record FaceSegmentationResult(
    byte[] Content,
    string ContentType,
    string FileName);
