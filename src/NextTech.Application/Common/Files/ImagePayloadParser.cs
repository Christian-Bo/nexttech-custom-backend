using NextTech.Domain.Exceptions;

namespace NextTech.Application.Common.Files;

public static class ImagePayloadParser
{
    private static readonly HashSet<string> MimePermitidos =
    [
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp"
    ];

    private const int TamanoMaximoBytes = 2 * 1024 * 1024;

    public static (byte[] Datos, string Extension, string Mime) Parse(
        string imagenBase64,
        string nombreArchivo,
        string tipoMime)
    {
        if (string.IsNullOrWhiteSpace(imagenBase64))
        {
            throw new BusinessRuleException("La imagen es obligatoria.");
        }

        var mime = (tipoMime ?? string.Empty).Trim().ToLowerInvariant();
        if (mime == "image/jpg")
        {
            mime = "image/jpeg";
        }

        if (!MimePermitidos.Contains(mime))
        {
            throw new BusinessRuleException(
                "Solo se permiten imágenes PNG, JPEG o WEBP.");
        }

        byte[] datos;
        try
        {
            datos = Convert.FromBase64String(imagenBase64.Trim());
        }
        catch (FormatException)
        {
            throw new BusinessRuleException("La imagen no es un Base64 válido.");
        }

        if (datos.Length == 0 || datos.Length > TamanoMaximoBytes)
        {
            throw new BusinessRuleException(
                "La imagen debe pesar más de 0 bytes y como máximo 2 MB.");
        }

        var extension = Path.GetExtension(nombreArchivo);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = mime switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }

        return (datos, extension.ToLowerInvariant(), mime);
    }
}
