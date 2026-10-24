using NextTech.Domain.Exceptions;

namespace NextTech.Application.Modules.Personalization;

public static class NfcUrlValidator
{
    public const int MaxLength = 2048;
    public const string JsonPropertyName = "nfcUrl";

    public const string MensajeObligatoria = "La URL del NFC debe ser un enlace https válido.";
    public const string MensajeDemasiadoLarga =
        "La URL del NFC es demasiado larga. Usa un enlace más corto (máximo 2048 caracteres).";

    public static string? Normalizar(string? nfcUrl)
    {
        if (string.IsNullOrWhiteSpace(nfcUrl))
        {
            return null;
        }

        var trimmed = nfcUrl.Trim();
        if (trimmed.Length > MaxLength)
        {
            throw new BusinessRuleException(MensajeDemasiadoLarga);
        }

        var candidata = CompletarHttps(trimmed);
        if (candidata.Length > MaxLength)
        {
            throw new BusinessRuleException(MensajeDemasiadoLarga);
        }

        if (!Uri.TryCreate(candidata, UriKind.Absolute, out var uri) ||
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new BusinessRuleException(MensajeObligatoria);
        }

        return uri.GetLeftPart(UriPartial.Query);
    }

    private static string CompletarHttps(string trimmed)
    {
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) &&
            !string.IsNullOrWhiteSpace(uri.Scheme))
        {
            if (string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                var separador = trimmed.IndexOf("://", StringComparison.Ordinal);
                if (separador < 0)
                {
                    throw new BusinessRuleException(MensajeObligatoria);
                }

                return "https://" + trimmed[(separador + 3)..];
            }

            throw new BusinessRuleException(MensajeObligatoria);
        }

        if (trimmed.Contains("://", StringComparison.Ordinal))
        {
            throw new BusinessRuleException(MensajeObligatoria);
        }

        return "https://" + trimmed.TrimStart('/');
    }
}
