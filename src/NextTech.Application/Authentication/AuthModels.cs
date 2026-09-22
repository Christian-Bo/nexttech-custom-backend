namespace NextTech.Application.Authentication;

public static class ActorTypes
{
    public const string Buyer = "buyer";
    public const string Internal = "internal";
}

public static class InternalRoles
{
    public const string Admin = "ADMIN";
    public const string Supervisor = "SUPERVISOR";
    public const string DeliveryDriver = "REPARTIDOR";
}

public sealed record AccessTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string ActorType,
    bool MustChangePassword = false);

public sealed record BuyerProfile(
    long IdUsuario,
    string Correo,
    string? Telefono,
    DateTime? FechaNacimiento,
    string Nickname,
    bool NotificaEmail,
    bool NotificaWhatsApp,
    bool Activo,
    bool Bloqueado);

public sealed record BuyerAuthRecord(
    long IdUsuario,
    string Correo,
    string? Telefono,
    DateTime? FechaNacimiento,
    string Nickname,
    string PasswordHash,
    bool NotificaEmail,
    bool NotificaWhatsApp,
    bool Activo,
    bool Bloqueado,
    int IntentosFallidos,
    long? IdFotoOriginal,
    long? IdFotoModificada);

public sealed record InternalUserAuthRecord(
    int IdUsuarioInterno,
    string Correo,
    string PasswordHash,
    string Role,
    bool Activo,
    bool DebeCambiarPassword,
    int IntentosFallidos,
    DateTime? BloqueadoHasta);

public enum PasswordCheckResult
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2,
    UnsupportedLegacyFormat = 3
}

