using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Interfaces;
using NextTech.Application.Modules.Auth;

namespace NextTech.UnitTests;

public sealed class InternalAuthServiceTests
{
    [Fact]
    public async Task Login_FifthFailedAttemptBlocksForFifteenMinutes()
    {
        var repo = new StubInternalAuthRepository(AuthUser(failedAttempts: 4));
        var service = new InternalAuthService(repo, new StubPasswordService(PasswordCheckResult.Failed), new StubTokenService());

        await Assert.ThrowsAsync<AppUnauthorizedException>(() => service.LoginAsync(
            new InternalLoginRequest("admin@example.test", "Wrong123"),
            "127.0.0.1",
            CancellationToken.None));

        Assert.Equal(5, repo.LastFailedAttempts);
        Assert.NotNull(repo.LastBlockedUntil);
        Assert.InRange(repo.LastBlockedUntil!.Value, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }

    [Fact]
    public async Task Login_RehashNeededDoesNotClearMandatoryPasswordChange()
    {
        var repo = new StubInternalAuthRepository(AuthUser(mustChangePassword: true));
        var service = new InternalAuthService(
            repo,
            new StubPasswordService(PasswordCheckResult.SuccessRehashNeeded),
            new StubTokenService());

        var result = await service.LoginAsync(
            new InternalLoginRequest("admin@example.test", "Current123"),
            null,
            CancellationToken.None);

        Assert.True(repo.UpgradePasswordHashCalled);
        Assert.False(repo.ChangePasswordCalled);
        Assert.True(result.MustChangePassword);
    }

    [Fact]
    public async Task ChangePasswordReturnsFreshSessionAndClearsMandatoryChange()
    {
        var repo = new StubInternalAuthRepository(AuthUser(mustChangePassword: true));
        var passwords = new SequencedPasswordService(
            PasswordCheckResult.Success,
            PasswordCheckResult.Failed);
        var service = new InternalAuthService(repo, passwords, new StubTokenService());

        var result = await service.ChangePasswordAsync(
            7,
            new ChangeInternalPasswordRequest("Current123", "NewPassword123"),
            "127.0.0.1",
            CancellationToken.None);

        Assert.True(repo.ChangePasswordCalled);
        Assert.False(result.MustChangePassword);
        Assert.False(result.User.MustChangePassword);
    }

    [Fact]
    public async Task LoginUnknownAccountIsAuditedWithoutCreatingUserState()
    {
        var repo = new StubInternalAuthRepository(null);
        var service = new InternalAuthService(repo, new StubPasswordService(PasswordCheckResult.Failed), new StubTokenService());

        await Assert.ThrowsAsync<AppUnauthorizedException>(() => service.LoginAsync(
            new InternalLoginRequest("missing@example.test", "Wrong123"),
            "127.0.0.1",
            CancellationToken.None));

        Assert.True(repo.UnknownFailedLoginAudited);
    }

    private static InternalUserAuthRecord AuthUser(int failedAttempts = 0, bool mustChangePassword = false)
        => new(7, "admin@example.test", "hash", InternalRoles.Admin, true, mustChangePassword, failedAttempts, null);

    private static InternalUserInfo Profile(bool mustChangePassword = false)
        => new(
            7,
            "Admin",
            "NextTech",
            "Admin NextTech",
            "admin@example.test",
            new InternalRoleInfo(1, InternalRoles.Admin, "Administrador"),
            true,
            mustChangePassword,
            0,
            null,
            false,
            null,
            DateTime.UtcNow,
            null,
            null);

    private sealed class StubInternalAuthRepository(InternalUserAuthRecord? initial) : IInternalAuthRepository
    {
        private InternalUserAuthRecord? _user = initial;

        public int? LastFailedAttempts { get; private set; }
        public DateTime? LastBlockedUntil { get; private set; }
        public bool UpgradePasswordHashCalled { get; private set; }
        public bool ChangePasswordCalled { get; private set; }
        public bool UnknownFailedLoginAudited { get; private set; }

        public Task<InternalUserAuthRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken ct)
            => Task.FromResult(_user);

        public Task<InternalUserAuthRecord?> FindByIdAsync(int userId, CancellationToken ct)
            => Task.FromResult(_user);

        public Task<InternalUserInfo?> FindProfileByIdAsync(int userId, CancellationToken ct)
            => Task.FromResult<InternalUserInfo?>(_user is null ? null : Profile(_user.DebeCambiarPassword));

        public Task RegisterUnknownFailedLoginAsync(string? ip, CancellationToken ct)
        {
            UnknownFailedLoginAudited = true;
            return Task.CompletedTask;
        }

        public Task RegisterRejectedLoginAsync(int userId, string reason, string? ip, CancellationToken ct)
            => Task.CompletedTask;

        public Task RegisterFailedLoginAsync(int userId, int newFailedAttempts, DateTime? blockedUntil, string? ip, CancellationToken ct)
        {
            LastFailedAttempts = newFailedAttempts;
            LastBlockedUntil = blockedUntil;
            return Task.CompletedTask;
        }

        public Task RegisterSuccessfulLoginAsync(int userId, string? ip, CancellationToken ct)
        {
            if (_user is not null)
                _user = _user with { IntentosFallidos = 0, BloqueadoHasta = null };
            return Task.CompletedTask;
        }

        public Task UpgradePasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
        {
            UpgradePasswordHashCalled = true;
            if (_user is not null)
                _user = _user with { PasswordHash = passwordHash };
            return Task.CompletedTask;
        }

        public Task ChangePasswordAsync(int userId, string passwordHash, string? ip, CancellationToken ct)
        {
            ChangePasswordCalled = true;
            if (_user is not null)
                _user = _user with { PasswordHash = passwordHash, DebeCambiarPassword = false };
            return Task.CompletedTask;
        }
    }

    private sealed class StubPasswordService(PasswordCheckResult result) : IPasswordService
    {
        public string Hash(string password) => "new-hash";
        public PasswordCheckResult Verify(string hash, string providedPassword) => result;
    }

    private sealed class SequencedPasswordService : IPasswordService
    {
        private readonly Queue<PasswordCheckResult> _results;

        public SequencedPasswordService(params PasswordCheckResult[] results)
        {
            _results = new Queue<PasswordCheckResult>(results);
        }

        public string Hash(string password) => "new-hash";
        public PasswordCheckResult Verify(string hash, string providedPassword) => _results.Dequeue();
    }

    private sealed class StubTokenService : ITokenService
    {
        public AccessTokenResult CreateBuyerToken(BuyerProfile buyer) => throw new NotSupportedException();

        public AccessTokenResult CreateInternalToken(InternalUserAuthRecord user)
            => new("token", DateTime.UtcNow.AddHours(1), ActorTypes.Internal, user.DebeCambiarPassword);
    }
}
