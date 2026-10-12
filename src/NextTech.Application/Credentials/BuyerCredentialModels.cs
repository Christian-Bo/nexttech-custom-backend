namespace NextTech.Application.Credentials;

public static class BuyerCredentialDesignCatalog
{
    public const string DefaultTemplateId = "NEXTTECH_BLUE";

    public static readonly IReadOnlyList<CredentialTemplateOption> Templates =
    [
        new("NEXTTECH_BLUE", "NextTech Blue", "Diseño claro con identidad azul de NextTech."),
        new("NEXTTECH_DARK", "NextTech Dark", "Diseño oscuro de alto contraste."),
        new("NEXTTECH_PURPLE", "NextTech Purple", "Diseño moderno con acento púrpura.")
    ];

    public static readonly IReadOnlyList<CredentialStickerOption> Stickers =
    [
        new("TECH", "Tech"),
        new("GAMER", "Gamer"),
        new("CODE", "Code"),
        new("STAR", "Star")
    ];

    public static readonly IReadOnlyList<string> StickerSlots =
    [
        "TOP_LEFT",
        "TOP_RIGHT",
        "BOTTOM_LEFT",
        "BOTTOM_RIGHT"
    ];

    public static CredentialCustomization Default =>
        new(DefaultTemplateId, []);

    public static CredentialCustomization Normalize(CredentialCustomizationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var templateId = string.IsNullOrWhiteSpace(request.TemplateId)
            ? DefaultTemplateId
            : request.TemplateId.Trim().ToUpperInvariant();

        if (!Templates.Any(x => string.Equals(x.Id, templateId, StringComparison.Ordinal)))
            throw new NextTech.Application.Common.AppValidationException("La plantilla de credencial no es válida.");

        var rawStickers = request.Stickers ?? [];
        if (rawStickers.Count > 4)
            throw new NextTech.Application.Common.AppValidationException("La credencial admite como máximo 4 stickers.");

        var normalized = new List<CredentialStickerPlacement>(rawStickers.Count);
        var usedSlots = new HashSet<string>(StringComparer.Ordinal);

        foreach (var sticker in rawStickers)
        {
            if (sticker is null)
                throw new NextTech.Application.Common.AppValidationException("La configuración de stickers no es válida.");

            var stickerId = sticker.StickerId?.Trim().ToUpperInvariant() ?? string.Empty;
            var slot = sticker.Slot?.Trim().ToUpperInvariant() ?? string.Empty;

            if (!Stickers.Any(x => string.Equals(x.Id, stickerId, StringComparison.Ordinal)))
                throw new NextTech.Application.Common.AppValidationException($"El sticker '{stickerId}' no está permitido.");
            if (!StickerSlots.Contains(slot, StringComparer.Ordinal))
                throw new NextTech.Application.Common.AppValidationException($"La posición '{slot}' no está permitida.");
            if (!usedSlots.Add(slot))
                throw new NextTech.Application.Common.AppValidationException("No se puede utilizar dos veces la misma posición de sticker.");

            normalized.Add(new CredentialStickerPlacement(stickerId, slot));
        }

        return new CredentialCustomization(templateId, normalized);
    }
}

public sealed record CredentialTemplateOption(
    string Id,
    string Name,
    string Description);

public sealed record CredentialStickerOption(
    string Id,
    string Name);

public sealed record CredentialStickerPlacementRequest(
    string? StickerId,
    string? Slot);

public sealed record CredentialCustomizationRequest(
    string? TemplateId,
    IReadOnlyList<CredentialStickerPlacementRequest>? Stickers);

public sealed record CredentialStickerPlacement(
    string StickerId,
    string Slot);

public sealed record CredentialCustomization(
    string TemplateId,
    IReadOnlyList<CredentialStickerPlacement> Stickers);

public sealed record CredentialCustomizationContext(
    string Nickname,
    string Role,
    string PortraitEndpoint,
    CredentialCustomization Current,
    IReadOnlyList<CredentialTemplateOption> Templates,
    IReadOnlyList<CredentialStickerOption> Stickers,
    IReadOnlyList<string> StickerSlots);

public sealed record BuyerCredentialPdfData(
    long BuyerId,
    string Nickname,
    string Role,
    byte[] PortraitContent,
    string PortraitContentType,
    string QrCredential,
    DateTimeOffset IssuedAtUtc,
    CredentialCustomization Customization,
    bool IsPreview = false);

public sealed record BuyerCredentialDocument(
    byte[] Content,
    string ContentType,
    string FileName,
    DateTimeOffset IssuedAtUtc);
