using NextTech.Application.Authentication;

namespace NextTech.Application.Interfaces;

public interface IPasswordService
{
    string Hash(string password);
    PasswordCheckResult Verify(string hash, string providedPassword);
}

public interface ITokenService
{
    AccessTokenResult CreateBuyerToken(BuyerProfile buyer);
    AccessTokenResult CreateInternalToken(InternalUserAuthRecord user);
}

public interface IInternalAuthRepository
{
    Task<InternalUserAuthRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken ct);
    Task<InternalUserAuthRecord?> FindByIdAsync(int userId, CancellationToken ct);
    Task RegisterFailedLoginAsync(int userId, int newFailedAttempts, DateTime? blockedUntil, string? ip, CancellationToken ct);
    Task RegisterSuccessfulLoginAsync(int userId, string? ip, CancellationToken ct);
    Task ChangePasswordAsync(int userId, string passwordHash, CancellationToken ct);
}

public interface ICentralIdentityGateway
{
    Task<BuyerAuthRecord?> FindByIdentifierAsync(string identifier, CancellationToken ct);
    Task<BuyerAuthRecord?> FindByIdAsync(long idUsuario, CancellationToken ct);
    Task<BuyerAuthRecord?> FindByQrHashAsync(string qrHash, CancellationToken ct);
    Task<long> RegisterAsync(BuyerRegistrationData data, CancellationToken ct);
    Task SetPasswordHashAsync(long idUsuario, string passwordHash, bool unlock, CancellationToken ct);
    Task RegisterFailedLoginAsync(long? idUsuario, string identifier, string method, string reason, CancellationToken ct);
    Task RegisterSuccessfulLoginAsync(long idUsuario, string identifier, string method, CancellationToken ct);
    Task<bool> VerifyLegacyPasswordAsync(string identifier, string password, CancellationToken ct);
    Task SetQrHashAsync(long idUsuario, string qrHash, CancellationToken ct);
    Task CreateRecoveryTokenAsync(long idUsuario, string tokenHash, DateTimeOffset expiresAt, CancellationToken ct);
    Task<long?> FindValidRecoveryUserAsync(string tokenHash, CancellationToken ct);
    Task ConsumeRecoveryTokenAsync(string tokenHash, CancellationToken ct);
}

public sealed record BuyerRegistrationData(
    string Correo,
    string Telefono,
    DateTime? FechaNacimiento,
    string Nickname,
    string PasswordHash,
    string QrHash,
    bool NotificaEmail,
    bool NotificaWhatsApp);

public interface IRegistrationNotificationSender
{
    Task SendRegistrationCredentialAsync(
        string email,
        string nickname,
        string qrCredential,
        CancellationToken ct);
}

public interface IRecoveryNotificationSender
{
    Task SendPasswordRecoveryAsync(
        string email,
        string rawToken,
        DateTimeOffset expiresAt,
        CancellationToken ct);
}
