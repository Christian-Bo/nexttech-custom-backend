using System.Text.Json;
using System.Text.Json.Nodes;

namespace NextTech.Application.Modules.Personalization;

public static class NfcUrlJson
{
    public static string Inyectar(string configuracionJson, string nfcUrl)
    {
        var node = JsonNode.Parse(configuracionJson) as JsonObject
            ?? throw new JsonException("La configuración de la zona debe ser un objeto JSON.");

        node[NfcUrlValidator.JsonPropertyName] = nfcUrl;
        return node.ToJsonString();
    }

    public static string? Extraer(string? configuracionJson)
    {
        if (string.IsNullOrWhiteSpace(configuracionJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(configuracionJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                !document.RootElement.TryGetProperty(NfcUrlValidator.JsonPropertyName, out var value) ||
                value.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var url = value.GetString();
            return string.IsNullOrWhiteSpace(url) ? null : url.Trim();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static string? ExtraerPrimera(IEnumerable<string> configuracionesJson)
    {
        foreach (var json in configuracionesJson)
        {
            var url = Extraer(json);
            if (!string.IsNullOrWhiteSpace(url))
            {
                return url;
            }
        }

        return null;
    }
}
