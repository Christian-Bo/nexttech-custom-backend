using System.Text;

namespace NextTech.Application.Common;

/// <summary>
/// Normalizes Guatemalan numbers only for the WhatsApp provider boundary.
/// Buyer phone values stored in Oracle are intentionally left unchanged.
/// </summary>
public static class GuatemalaPhoneNumber
{
    private const string CountryCode = "502";

    public static bool TryNormalizeForWhatsApp(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        var phone = input.Trim();
        if (phone.Length > 25)
            return false;

        var digits = new StringBuilder(11);
        for (var i = 0; i < phone.Length; i++)
        {
            var character = phone[i];
            if (character is >= '0' and <= '9')
            {
                digits.Append(character);
            }
            else if (char.IsWhiteSpace(character) || character is '-' or '(' or ')' or '.')
            {
                continue;
            }
            else if (character == '+' && i == 0)
            {
                continue;
            }
            else
            {
                return false;
            }
        }

        var number = digits.ToString();
        if (number.Length == 8)
        {
            normalized = CountryCode + number;
            return true;
        }

        if (number.Length == 11 && number.StartsWith(CountryCode, StringComparison.Ordinal))
        {
            normalized = number;
            return true;
        }

        return false;
    }
}
