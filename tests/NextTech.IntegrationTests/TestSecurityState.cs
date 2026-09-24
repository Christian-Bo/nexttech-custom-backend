using NextTech.Application.Authentication;
using NextTech.Application.Interfaces;

namespace NextTech.IntegrationTests;

public sealed class TestSecurityState :
    ICentralIdentityGateway,
    IInternalAuthRepository,
    IInternalUserAdministrationRepository
{
    private readonly IReadOnlyList<InternalRoleInfo> _roles =
    [
        new(1, InternalRoles.Admin, "Administrador"),
        new(2, InternalRoles.Supervisor, "Supervisor"),
        new(3, InternalRoles.DeliveryDriver, "Repartidor")
    ];

    public BuyerAuthRecord Buyer { get; private set; } = CreateBuyer();

    public InternalUserAuthRecord InternalUser { get; private set; } = CreateInternalUser();

    public void SetBuyer(bool active = true, bool blocked = false)
        => Buyer = CreateBuyer(active, blocked);

    public void SetInternalUser(
        string role = InternalRoles.Admin,
        bool active = true,
        bool mustChangePassword = false,
        DateTime? blockedUntil = null)
        => InternalUser = CreateInternalUser(role, active, mustChangePassword, blockedUntil);

    public static BuyerAuthRecord CreateBuyer(bool active = true, bool blocked = false)
        => new(
            46,
            "buyer.integration@nexttech.test",
            "+50255550101",
            new DateTime(2000, 1, 1),
            "buyer-integration",
            "hash",
            true,
            false,
            active,
            blocked,
            0,
            10,
            11);

    public static InternalUserAuthRecord CreateInternalUser(
        string role = InternalRoles.Admin,
        bool active = true,
        bool mustChangePassword = false,
        DateTime? blockedUntil = null)
        => new(
            7,
            "internal.integration@nexttech.test",
            "hash",
            role,
            active,
            mustChangePassword,
            0,
            blockedUntil);

    public static InternalUserInfo CreateInternalProfile(InternalUserAuthRecord user)
    {
        var role = user.Role switch
        {
            InternalRoles.Admin => new InternalRoleInfo(1, InternalRoles.Admin, "Administrador"),
            InternalRoles.Supervisor => new InternalRoleInfo(2, InternalRoles.Supervisor, "Supervisor"),
            InternalRoles.DeliveryDriver => new InternalRoleInfo(3, InternalRoles.DeliveryDriver, "Repartidor"),
            _ => new InternalRoleInfo(99, user.Role, user.Role)
        };

        return new InternalUserInfo(
            user.IdUsuarioInterno,
            "Integration",
            "User",
            "Integration User",
            user.Correo,
            role,
            user.Activo,
            user.DebeCambiarPassword,
            user.IntentosFallidos,
            user.BloqueadoHasta,
            user.BloqueadoHasta is not null && user.BloqueadoHasta > DateTime.UtcNow,
            null,
            DateTime.UtcNow.AddDays(-1),
            null,
            null);
    }

    Task<BuyerAuthRecord?> ICentralIdentityGateway.FindByIdentifierAsync(string identifier, CancellationToken ct)
        => Task.FromResult<BuyerAuthRecord?>(Buyer);

    Task<BuyerAuthRecord?> ICentralIdentityGateway.FindByIdAsync(long idUsuario, CancellationToken ct)
        => Task.FromResult<BuyerAuthRecord?>(idUsuario == Buyer.IdUsuario ? Buyer : null);

    Task<BuyerAuthRecord?> ICentralIdentityGateway.FindByQrHashAsync(string qrHash, CancellationToken ct)
        => Task.FromResult<BuyerAuthRecord?>(null);

    Task<long> ICentralIdentityGateway.RegisterAsync(BuyerRegistrationData data, CancellationToken ct)
        => throw new NotSupportedException();

    Task ICentralIdentityGateway.SetPasswordHashAsync(long idUsuario, string passwordHash, bool unlock, CancellationToken ct)
        => Task.CompletedTask;

    Task ICentralIdentityGateway.RegisterFailedLoginAsync(long? idUsuario, string identifier, string method, string reason, CancellationToken ct)
        => Task.CompletedTask;

    Task ICentralIdentityGateway.RegisterSuccessfulLoginAsync(long idUsuario, string identifier, string method, CancellationToken ct)
        => Task.CompletedTask;

    Task<bool> ICentralIdentityGateway.VerifyLegacyPasswordAsync(string identifier, string password, CancellationToken ct)
        => Task.FromResult(false);

    Task ICentralIdentityGateway.SetQrHashAsync(long idUsuario, string qrHash, CancellationToken ct)
        => Task.CompletedTask;

    Task ICentralIdentityGateway.CreateRecoveryTokenAsync(long idUsuario, string tokenHash, DateTimeOffset expiresAt, CancellationToken ct)
        => Task.CompletedTask;

    Task<long?> ICentralIdentityGateway.FindValidRecoveryUserAsync(string tokenHash, CancellationToken ct)
        => Task.FromResult<long?>(null);

    Task ICentralIdentityGateway.ConsumeRecoveryTokenAsync(string tokenHash, CancellationToken ct)
        => Task.CompletedTask;

    Task<InternalUserAuthRecord?> IInternalAuthRepository.FindByEmailAsync(string normalizedEmail, CancellationToken ct)
        => Task.FromResult<InternalUserAuthRecord?>(InternalUser);

    Task<InternalUserAuthRecord?> IInternalAuthRepository.FindByIdAsync(int userId, CancellationToken ct)
        => Task.FromResult<InternalUserAuthRecord?>(userId == InternalUser.IdUsuarioInterno ? InternalUser : null);

    Task<InternalUserInfo?> IInternalAuthRepository.FindProfileByIdAsync(int userId, CancellationToken ct)
        => Task.FromResult<InternalUserInfo?>(
            userId == InternalUser.IdUsuarioInterno ? CreateInternalProfile(InternalUser) : null);

    Task IInternalAuthRepository.RegisterUnknownFailedLoginAsync(string? ip, CancellationToken ct)
        => Task.CompletedTask;

    Task IInternalAuthRepository.RegisterRejectedLoginAsync(int userId, string reason, string? ip, CancellationToken ct)
        => Task.CompletedTask;

    Task IInternalAuthRepository.RegisterFailedLoginAsync(int userId, int newFailedAttempts, DateTime? blockedUntil, string? ip, CancellationToken ct)
        => Task.CompletedTask;

    Task IInternalAuthRepository.RegisterSuccessfulLoginAsync(int userId, string? ip, CancellationToken ct)
        => Task.CompletedTask;

    Task IInternalAuthRepository.UpgradePasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
        => Task.CompletedTask;

    Task IInternalAuthRepository.ChangePasswordAsync(int userId, string passwordHash, string? ip, CancellationToken ct)
        => Task.CompletedTask;

    Task<IReadOnlyList<InternalRoleInfo>> IInternalUserAdministrationRepository.GetRolesAsync(CancellationToken ct)
        => Task.FromResult(_roles);

    Task<InternalRoleInfo?> IInternalUserAdministrationRepository.FindRoleByCodeAsync(string roleCode, CancellationToken ct)
        => Task.FromResult<InternalRoleInfo?>(_roles.FirstOrDefault(r => r.Code == roleCode));

    Task<InternalUserInfo?> IInternalUserAdministrationRepository.FindByIdAsync(int userId, CancellationToken ct)
        => Task.FromResult<InternalUserInfo?>(
            userId == InternalUser.IdUsuarioInterno ? CreateInternalProfile(InternalUser) : null);

    Task<bool> IInternalUserAdministrationRepository.EmailExistsAsync(string normalizedEmail, int? excludingUserId, CancellationToken ct)
        => Task.FromResult(false);

    Task<int> IInternalUserAdministrationRepository.CountActiveAdminsAsync(CancellationToken ct)
        => Task.FromResult(InternalUser.Activo && InternalUser.Role == InternalRoles.Admin ? 1 : 0);

    Task<PagedResult<InternalUserInfo>> IInternalUserAdministrationRepository.SearchAsync(InternalUserListRequest request, CancellationToken ct)
    {
        IReadOnlyList<InternalUserInfo> items = [CreateInternalProfile(InternalUser)];
        return Task.FromResult(new PagedResult<InternalUserInfo>(items, 1, request.PageSize, 1, 1));
    }

    Task<InternalUserInfo> IInternalUserAdministrationRepository.CreateAsync(CreateInternalUserData data, int actorUserId, string? ip, CancellationToken ct)
        => throw new NotSupportedException();

    Task<InternalUserInfo> IInternalUserAdministrationRepository.UpdateAsync(int userId, UpdateInternalUserData data, int actorUserId, string? ip, CancellationToken ct)
        => throw new NotSupportedException();

    Task<InternalUserInfo> IInternalUserAdministrationRepository.SetActiveAsync(int userId, bool active, int actorUserId, string? ip, CancellationToken ct)
        => throw new NotSupportedException();

    Task<InternalUserInfo> IInternalUserAdministrationRepository.UnlockAsync(int userId, int actorUserId, string? ip, CancellationToken ct)
        => throw new NotSupportedException();

    Task<InternalUserInfo> IInternalUserAdministrationRepository.ResetPasswordAsync(int userId, string passwordHash, int actorUserId, string? ip, CancellationToken ct)
        => throw new NotSupportedException();
}
