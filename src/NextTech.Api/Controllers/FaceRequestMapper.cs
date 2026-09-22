using NextTech.Application.Common;
using NextTech.Application.Face;

namespace NextTech.Api.Controllers;

internal static class FaceRequestMapper
{
    private const long MaxImageBytes = 5L * 1024 * 1024;

    public static async Task<FaceImage> ReadImageAsync(
        IFormFile? file,
        string fieldName,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new AppValidationException($"La imagen {fieldName} es obligatoria.");

        if (file.Length > MaxImageBytes)
            throw new AppValidationException($"La imagen {fieldName} no puede exceder 5 MB.");

        if (string.IsNullOrWhiteSpace(file.ContentType) ||
            !file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new AppValidationException($"El archivo {fieldName} debe ser una imagen.");
        }

        await using var input = file.OpenReadStream();
        using var memory = new MemoryStream((int)file.Length);
        await input.CopyToAsync(memory, ct);

        return new FaceImage(memory.ToArray(), file.ContentType, file.FileName);
    }
}
