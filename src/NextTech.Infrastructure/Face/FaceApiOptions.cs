namespace NextTech.Infrastructure.Face;

public sealed class FaceApiOptions
{
    public const string SectionName = "FaceApi";

    public string BaseUrl { get; init; } = string.Empty;
    public string EnrollPath { get; init; } = "/api/v1/faces/enroll";
    public string SegmentPath { get; init; } = "/api/v1/faces/segment/image";
    public string LivenessPath { get; init; } = "/api/v1/faces/liveness/challenge";
    public string VerifyPath { get; init; } = "/api/v1/faces/verify-template-live";
    public string? ApiKey { get; init; }
    public string ApiKeyHeader { get; init; } = "X-Internal-Api-Key";
    public int TimeoutSeconds { get; init; } = 30;
}
