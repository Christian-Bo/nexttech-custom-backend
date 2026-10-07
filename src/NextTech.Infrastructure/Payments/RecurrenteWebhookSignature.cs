using System.Security.Cryptography;
using System.Text;

namespace NextTech.Infrastructure.Payments;

public static class RecurrenteWebhookSignature
{
    public static bool IsValid(
        string rawBody,
        string? svixId,
        string? svixTimestamp,
        string? svixSignature,
        string signingSecret,
        TimeSpan maxAge)
    {
        if (string.IsNullOrWhiteSpace(rawBody) ||
            string.IsNullOrWhiteSpace(svixId) ||
            string.IsNullOrWhiteSpace(svixTimestamp) ||
            string.IsNullOrWhiteSpace(svixSignature) ||
            string.IsNullOrWhiteSpace(signingSecret))
        {
            return false;
        }

        if (!long.TryParse(svixTimestamp, out var unixSeconds))
        {
            return false;
        }

        var sentAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        var age = DateTimeOffset.UtcNow - sentAt;
        if (age > maxAge || age < TimeSpan.FromMinutes(-1))
        {
            return false;
        }

        var secretPart = signingSecret.StartsWith("whsec_", StringComparison.Ordinal)
            ? signingSecret["whsec_".Length..]
            : signingSecret;

        byte[] key;
        try
        {
            key = Convert.FromBase64String(secretPart);
        }
        catch (FormatException)
        {
            return false;
        }

        var signedContent = $"{svixId}.{svixTimestamp}.{rawBody}";
        var expected = HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(signedContent));

        foreach (var candidate in svixSignature.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var value = candidate.Trim();
            if (value.StartsWith("v1,", StringComparison.Ordinal))
            {
                value = value[3..];
            }

            byte[] actual;
            try
            {
                actual = Convert.FromBase64String(value);
            }
            catch (FormatException)
            {
                continue;
            }

            if (CryptographicOperations.FixedTimeEquals(expected, actual))
            {
                return true;
            }
        }

        return false;
    }
}
