using NextTech.Application.Common;

namespace NextTech.Application.Authentication;

public static class PasswordPolicy
{
    public static void Validate(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 || password.Length > 128)
            throw new AppValidationException("La contraseña debe tener entre 8 y 128 caracteres.");

        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            throw new AppValidationException("La contraseña debe incluir mayúscula, minúscula y número.");
    }
}
