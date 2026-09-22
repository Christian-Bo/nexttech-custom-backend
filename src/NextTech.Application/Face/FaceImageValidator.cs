using NextTech.Application.Common;

namespace NextTech.Application.Face;

public static class FaceImageValidator
{
    public const int MaxImageBytes = 5 * 1024 * 1024;

    public static void Validate(FaceImage image)
    {
        if (image.Content.Length == 0)
            throw new AppValidationException("La imagen facial es obligatoria.");
        if (image.Content.Length > MaxImageBytes)
            throw new AppValidationException("La imagen facial no puede exceder 5 MB.");

        var contentType = image.ContentType.Trim().ToLowerInvariant();
        var valid = contentType switch
        {
            "image/jpeg" or "image/jpg" => IsJpeg(image.Content),
            "image/png" => IsPng(image.Content),
            "image/webp" => IsWebP(image.Content),
            _ => false
        };

        if (!valid)
            throw new AppValidationException("La imagen debe ser un archivo JPEG, PNG o WebP válido.");
    }

    private static bool IsJpeg(ReadOnlySpan<byte> bytes)
        => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> bytes)
        => bytes.Length >= 8 &&
           bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 &&
           bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A;

    private static bool IsWebP(ReadOnlySpan<byte> bytes)
        => bytes.Length >= 12 &&
           bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
           bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P';
}
