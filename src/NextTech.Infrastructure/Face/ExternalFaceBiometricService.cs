using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NextTech.Application.Common;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.Face;

/// <summary>
/// Adapter for the deployed NextTech Face API contract.
/// Protected templates and portrait base64 are parsed here and never exposed by API controllers.
/// </summary>
public sealed class ExternalFaceBiometricService(
    HttpClient client,
    IOptions<FaceApiOptions> options) : IFaceBiometricService
{
    private const string ImageField = "image";
    private const string TemplateField = "biometricTemplate";
    private const string ChallengeIdField = "challengeId";
    private const string NeutralImageField = "neutralImage";
    private const string ChallengeImageField = "challengeImage";

    private readonly FaceApiOptions _options = options.Value;

    public async Task<FaceChallengeResult> CreateLivenessChallengeAsync(CancellationToken ct)
    {
        using var response = await client.PostAsync(_options.LivenessPath, content: null, ct);
        await EnsureSuccessAsync(response, "generación de challenge de liveness", ct);

        using var json = await ReadJsonAsync(response, "generación de challenge de liveness", ct);
        var challenge = RequiredObject(json.RootElement, "challenge", "challenge de liveness");

        var id = RequiredString(challenge, "id", "challenge de liveness");
        var action = RequiredString(challenge, "action", "challenge de liveness");
        var instruction = RequiredString(challenge, "instruction", "challenge de liveness");
        var expiresAt = RequiredDateTimeOffset(challenge, "expiresAtUtc", "challenge de liveness");
        var expiresIn = RequiredInt32(challenge, "expiresInSeconds", "challenge de liveness");

        return new FaceChallengeResult(id, action, instruction, expiresAt, expiresIn);
    }

    public async Task<ProtectedFaceEnrollmentResult> CreateTemplateAsync(
        FaceImage image,
        CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        AddImage(form, ImageField, image);

        using var response = await client.PostAsync(_options.EnrollPath, form, ct);
        await EnsureSuccessAsync(response, "enrolamiento facial", ct);

        using var json = await ReadJsonAsync(response, "enrolamiento facial", ct);
        var root = json.RootElement;
        var portrait = RequiredObject(root, "portrait", "enrolamiento facial");
        var template = RequiredObject(root, "biometricTemplate", "enrolamiento facial");

        var encodedPortrait = RequiredString(portrait, "imageBase64", "enrolamiento facial");
        byte[] portraitBytes;
        try
        {
            portraitBytes = Convert.FromBase64String(encodedPortrait);
        }
        catch (FormatException)
        {
            throw new AppDependencyException("La Face API devolvió un retrato biométrico inválido.");
        }

        if (portraitBytes.Length == 0)
            throw new AppDependencyException("La Face API devolvió un retrato biométrico vacío.");

        var protectedTemplate = new ProtectedBiometricTemplate(
            RequiredString(template, "value", "enrolamiento facial"),
            RequiredString(template, "version", "enrolamiento facial"),
            RequiredString(template, "keyId", "enrolamiento facial"),
            RequiredString(template, "model", "enrolamiento facial"),
            RequiredInt32(template, "dimensions", "enrolamiento facial"),
            OptionalString(template, "embeddingSha256"));

        var processedPortrait = new ProcessedFacePortrait(
            portraitBytes,
            RequiredString(portrait, "mimeType", "enrolamiento facial"),
            RequiredInt32(portrait, "width", "enrolamiento facial"),
            RequiredInt32(portrait, "height", "enrolamiento facial"),
            OptionalString(portrait, "background"));

        string? qualityStatus = null;
        var warningCount = 0;
        if (TryGetProperty(root, "quality", out var quality) && quality.ValueKind == JsonValueKind.Object)
        {
            qualityStatus = OptionalString(quality, "status");
            if (TryGetProperty(quality, "issues", out var issues) && issues.ValueKind == JsonValueKind.Array)
                warningCount = issues.GetArrayLength();
        }

        return new ProtectedFaceEnrollmentResult(
            protectedTemplate,
            processedPortrait,
            OptionalDecimal(root, "faceScore"),
            qualityStatus,
            warningCount,
            OptionalString(root, "mensaje"));
    }

    public async Task<FaceVerificationResult> VerifyTemplateLiveAsync(
        string biometricTemplate,
        string challengeId,
        FaceImage neutralImage,
        FaceImage challengeImage,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(biometricTemplate))
            throw new AppDependencyException("No existe un template biométrico válido para realizar la verificación.");
        if (string.IsNullOrWhiteSpace(challengeId))
            throw new AppValidationException("El challengeId es obligatorio.");

        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(biometricTemplate), TemplateField);
        form.Add(new StringContent(challengeId), ChallengeIdField);
        AddImage(form, NeutralImageField, neutralImage);
        AddImage(form, ChallengeImageField, challengeImage);

        using var response = await client.PostAsync(_options.VerifyPath, form, ct);
        await EnsureSuccessAsync(response, "verificación facial con liveness", ct);

        using var json = await ReadJsonAsync(response, "verificación facial con liveness", ct);
        var root = json.RootElement;
        var authenticationPassed = RequiredBoolean(root, "authenticationPassed", "verificación facial con liveness");
        var decision = RequiredString(root, "decision", "verificación facial con liveness");
        var liveness = RequiredObject(root, "liveness", "verificación facial con liveness");

        bool? isMatch = null;
        decimal? similarity = null;
        decimal? threshold = null;
        decimal? decisionMargin = null;
        if (TryGetProperty(root, "verification", out var verification) &&
            verification.ValueKind == JsonValueKind.Object)
        {
            isMatch = OptionalBoolean(verification, "isMatch");
            similarity = OptionalDecimal(verification, "similarity");
            threshold = OptionalDecimal(verification, "threshold");
            decisionMargin = OptionalDecimal(verification, "decisionMargin");
        }

        return new FaceVerificationResult(
            authenticationPassed,
            decision,
            RequiredBoolean(liveness, "isLive", "liveness"),
            RequiredString(liveness, "decision", "liveness"),
            OptionalString(liveness, "reasonCode"),
            RequiredString(liveness, "action", "liveness"),
            OptionalDecimal(liveness, "yawDelta"),
            OptionalDecimal(liveness, "requiredYawDelta"),
            OptionalDecimal(liveness, "frameIdentitySimilarity"),
            OptionalDecimal(liveness, "frameIdentityThreshold"),
            isMatch,
            similarity,
            threshold,
            decisionMargin,
            OptionalString(root, "mensaje"));
    }

    public async Task<FaceSegmentationResult> SegmentForCardAsync(
        FaceImage image,
        CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        AddImage(form, ImageField, image);

        using var response = await client.PostAsync(_options.SegmentPath, form, ct);
        await EnsureSuccessAsync(response, "segmentación para carnet", ct);

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (string.IsNullOrWhiteSpace(mediaType) ||
            !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppDependencyException("La Face API devolvió un formato inválido durante la segmentación.");
        }

        var bytes = await response.Content.ReadAsByteArrayAsync(ct);
        if (bytes.Length == 0)
            throw new AppDependencyException("La Face API devolvió una imagen segmentada vacía.");

        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;
        fileName = string.IsNullOrWhiteSpace(fileName)
            ? BuildSegmentedFileName(image.FileName, mediaType)
            : SanitizeFileName(fileName.Trim('"'));

        return new FaceSegmentationResult(bytes, mediaType, fileName);
    }

    private static void AddImage(MultipartFormDataContent form, string fieldName, FaceImage image)
    {
        var content = new ByteArrayContent(image.Content);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse(image.ContentType);
        form.Add(content, fieldName, SanitizeFileName(image.FileName));
    }

    private static async Task<JsonDocument> ReadJsonAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken ct)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(ct);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        }
        catch (JsonException)
        {
            throw new AppDependencyException($"La Face API devolvió JSON inválido durante {operation}.");
        }
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        string operation,
        CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var problem = await TryReadProviderProblemAsync(response, ct);
        var status = response.StatusCode;

        if (status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            throw new AppDependencyException("La Face API rechazó las credenciales de integración o los scopes configurados.");

        if (status is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity)
        {
            throw new AppProviderRejectedException(
                problem.Code ?? "FACE_API_REQUEST_REJECTED",
                problem.Message ?? $"La Face API rechazó la solicitud de {operation}.",
                (int)status);
        }

        if (status == HttpStatusCode.Conflict)
        {
            throw new AppProviderRejectedException(
                problem.Code ?? "FACE_API_CONFLICT",
                problem.Message ?? $"Existe un conflicto durante {operation}.",
                StatusCodes.Status409Conflict);
        }

        throw new AppDependencyException($"La Face API respondió {(int)status} durante {operation}.");
    }

    private static async Task<(string? Code, string? Message)> TryReadProviderProblemAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        try
        {
            var raw = await response.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(raw) || raw.Length > 16_384)
                return (null, null);

            using var json = JsonDocument.Parse(raw);
            var root = json.RootElement;
            var code = OptionalString(root, "code");
            var message = FirstSafeError(root)
                ?? OptionalString(root, "detail")
                ?? OptionalString(root, "title");

            if (message is { Length: > 500 })
                message = null;

            return (code, message);
        }
        catch
        {
            return (null, null);
        }
    }

    private static string? FirstSafeError(JsonElement root)
    {
        if (!TryGetProperty(root, "errors", out var errors) || errors.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in errors.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var item in property.Value.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                    return item.GetString();
            }
        }

        return null;
    }

    private static JsonElement RequiredObject(JsonElement parent, string name, string operation)
    {
        if (!TryGetProperty(parent, name, out var value) || value.ValueKind != JsonValueKind.Object)
            throw new AppDependencyException($"La Face API devolvió un contrato inválido para {operation}.");
        return value;
    }

    private static string RequiredString(JsonElement parent, string name, string operation)
        => OptionalString(parent, name)
           ?? throw new AppDependencyException($"La Face API devolvió un contrato inválido para {operation}.");

    private static bool RequiredBoolean(JsonElement parent, string name, string operation)
        => OptionalBoolean(parent, name)
           ?? throw new AppDependencyException($"La Face API devolvió un contrato inválido para {operation}.");

    private static int RequiredInt32(JsonElement parent, string name, string operation)
    {
        if (TryGetProperty(parent, name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            return number;
        throw new AppDependencyException($"La Face API devolvió un contrato inválido para {operation}.");
    }

    private static DateTimeOffset RequiredDateTimeOffset(JsonElement parent, string name, string operation)
    {
        var raw = RequiredString(parent, name, operation);
        if (DateTimeOffset.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value))
            return value;
        throw new AppDependencyException($"La Face API devolvió un contrato inválido para {operation}.");
    }

    private static string? OptionalString(JsonElement parent, string name)
    {
        if (!TryGetProperty(parent, name, out var value) || value.ValueKind != JsonValueKind.String)
            return null;
        return value.GetString();
    }

    private static bool? OptionalBoolean(JsonElement parent, string name)
    {
        if (!TryGetProperty(parent, name, out var value) ||
            value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            return null;
        return value.GetBoolean();
    }

    private static decimal? OptionalDecimal(JsonElement parent, string name)
    {
        if (!TryGetProperty(parent, name, out var value))
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String &&
            decimal.TryParse(value.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out number))
            return number;

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }
        }

        value = default;
        return false;
    }

    private static string BuildSegmentedFileName(string original, string contentType)
    {
        var stem = Path.GetFileNameWithoutExtension(SanitizeFileName(original));
        if (string.IsNullOrWhiteSpace(stem))
            stem = "portrait";

        var extension = contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/webp" => ".webp",
            _ => ".png"
        };

        return $"{stem}-segmented{extension}";
    }

    private static string SanitizeFileName(string fileName)
    {
        var safe = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safe) ? "face.jpg" : safe;
    }
}
