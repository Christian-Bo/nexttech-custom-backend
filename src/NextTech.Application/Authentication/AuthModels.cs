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

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Admin,
        Supervisor,
        DeliveryDriver
    };
}

public static class AuditActorIds
{
    public const int System = 1;
    public const int InternalUser = 2;
    public const int Buyer = 3;
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

