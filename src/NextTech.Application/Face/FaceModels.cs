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

public sealed record BuyerBiometricCredential(
    long BuyerId,
    string BiometricTemplate,
    string TemplateVersion,
    string TemplateKeyId,
    string TemplateModel,
    int TemplateDimensions,
    string? EmbeddingSha256,
    byte[] PortraitContent,
    string PortraitContentType,
    int PortraitWidth,
    int PortraitHeight,
    string? PortraitBackground,
    DateTimeOffset EnrolledAtUtc,
    DateTimeOffset? UpdatedAtUtc);

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

public sealed record FaceSegmentationResult(
    byte[] Content,
    string ContentType,
    string FileName);
