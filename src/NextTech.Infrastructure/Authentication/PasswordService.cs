using Microsoft.AspNetCore.Identity;
using NextTech.Application.Authentication;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.Authentication;

public sealed class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _hasher = new();
    private static readonly object User = new();

    public string Hash(string password) => _hasher.HashPassword(User, password);

    public PasswordCheckResult Verify(string hash, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hash)) return PasswordCheckResult.Failed;

        // El esquema Oracle histórico usaba SHA-256 hexadecimal de 64 caracteres.
        // No lo reproducimos en C#: esa compatibilidad se delega al PKG_LOGIN de Oracle
        // y, tras un login correcto, se migra a ASP.NET Core Identity V3.
        if (hash.Length == 64 && hash.All(Uri.IsHexDigit))
            return PasswordCheckResult.UnsupportedLegacyFormat;

        try
        {
            return _hasher.VerifyHashedPassword(User, hash, providedPassword) switch
            {
                PasswordVerificationResult.Success => PasswordCheckResult.Success,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordCheckResult.SuccessRehashNeeded,
                _ => PasswordCheckResult.Failed
            };
        }
        catch (FormatException)
        {
            return PasswordCheckResult.Failed;
        }
    }
}
