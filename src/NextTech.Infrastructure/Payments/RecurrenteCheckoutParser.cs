using System.Text.Json;
using System.Text.Json.Serialization;
using NextTech.Application.Payments;

namespace NextTech.Infrastructure.Payments;

public static class RecurrenteCheckoutParser
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static RecurrenteCheckoutSession? TryParse(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        RecurrenteCheckoutBody? body;
        try
        {
            body = JsonSerializer.Deserialize<RecurrenteCheckoutBody>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }

        if (body is null || string.IsNullOrWhiteSpace(body.Id))
        {
            return null;
        }

        return new RecurrenteCheckoutSession(
            body.Id,
            body.CheckoutUrl ?? string.Empty,
            body.Status ?? string.Empty,
            ReadAmountInCents(body.TotalInCents, body.AmountInCents, body.SubtotalInCents),
            string.IsNullOrWhiteSpace(body.Currency) ? "GTQ" : body.Currency,
            ReadMetadata(body.Metadata));
    }

    public static int ReadAmountInCents(int totalInCents, int amountInCents, int subtotalInCents)
        => totalInCents > 0
            ? totalInCents
            : amountInCents > 0
                ? amountInCents
                : subtotalInCents;

    private static IReadOnlyDictionary<string, string> ReadMetadata(
        Dictionary<string, JsonElement>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return new Dictionary<string, string>();
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in metadata)
        {
            map[pair.Key] = pair.Value.ValueKind switch
            {
                JsonValueKind.String => pair.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => pair.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Null => string.Empty,
                _ => pair.Value.ToString()
            };
        }

        return map;
    }

    private sealed class RecurrenteCheckoutBody
    {
        public string? Id { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? Status { get; set; }
        public int AmountInCents { get; set; }
        public int TotalInCents { get; set; }
        public int SubtotalInCents { get; set; }
        public string? Currency { get; set; }
        public Dictionary<string, JsonElement>? Metadata { get; set; }
    }
}
