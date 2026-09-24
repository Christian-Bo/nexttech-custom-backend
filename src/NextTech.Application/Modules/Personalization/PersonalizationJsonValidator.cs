using System.Text.Json;
using NextTech.Domain.Exceptions;

namespace NextTech.Application.Modules.Personalization;

public static class PersonalizationJsonValidator
{
    public static void Validar(string configuracionJson)
    {
        if (string.IsNullOrWhiteSpace(configuracionJson))
        {
            throw new BusinessRuleException("La configuración de la zona no puede estar vacía.");
        }

        JsonElement root;
        try
        {
            using var document = JsonDocument.Parse(configuracionJson);
            root = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            throw new BusinessRuleException("La configuración de la zona no es un JSON válido.");
        }

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new BusinessRuleException("La configuración de la zona debe ser un objeto JSON.");
        }

        var tieneImagen = TieneValor(root, "imagen");
        var tieneTexto = TieneValor(root, "texto");
        var stickers = ContarStickers(root);

        if (!tieneImagen && !tieneTexto && stickers == 0)
        {
            throw new BusinessRuleException("Una zona no puede quedar vacía: agrega imagen, texto o stickers.");
        }

        if (stickers > 3)
        {
            throw new BusinessRuleException("Cada zona admite como máximo 3 stickers.");
        }
    }

    private static bool TieneValor(JsonElement root, string property)
    {
        if (!root.TryGetProperty(property, out var value))
        {
            return false;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Null => false,
            JsonValueKind.String => !string.IsNullOrWhiteSpace(value.GetString()),
            JsonValueKind.Object => value.EnumerateObject().Any(),
            JsonValueKind.Array => value.GetArrayLength() > 0,
            _ => true
        };
    }

    private static int ContarStickers(JsonElement root)
    {
        if (!root.TryGetProperty("stickers", out var stickers)
            || stickers.ValueKind != JsonValueKind.Array)
        {
            return 0;
        }

        return stickers.GetArrayLength();
    }
}
