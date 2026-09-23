using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Interfaces;
using NextTech.Application.Modules.Auth;

namespace NextTech.UnitTests;

public sealed class InternalUserAdministrationServiceTests
{
    [Fact]
    public async Task CreateReturnsFullUserWithRoleNameAndForcesPasswordChange()
    {
        var repo = new StubAdministrationRepository();
        var service = new InternalUserAdministrationService(repo, new StubPasswordService());

        var created = await service.CreateAsync(
            new CreateInternalUserRequest(" Ana ", " Pérez ", " ANA@EXAMPLE.TEST ", InternalRoles.Supervisor, "TempPassword1"),
            1,
            "127.0.0.1",
            CancellationToken.None);

        Assert.Equal("Ana Pérez", created.FullName);
        Assert.Equal("ana@example.test", created.Email);
        Assert.Equal(InternalRoles.Supervisor, created.Role.Code);
        Assert.Equal("Supervisor", created.Role.Name);
        Assert.True(created.MustChangePassword);
    }

    [Fact]
    public async Task DeactivateUsesLogicalDeletionAndNeverRemovesTheUser()
    {
        var repo = new StubAdministrationRepository();
        var service = new InternalUserAdministrationService(repo, new StubPasswordService());

        var result = await service.DeactivateAsync(2, 1, null, CancellationToken.None);

        Assert.False(result.IsActive);
        Assert.NotNull(result.DeactivatedAtUtc);
        Assert.True(repo.UserStillExists(2));
    }

    [Fact]
    public async Task AdminCannotDeactivateOwnAccount()
    {
        var service = new InternalUserAdministrationService(new StubAdministrationRepository(), new StubPasswordService());

        await Assert.ThrowsAsync<AppConflictException>(() => service.DeactivateAsync(
            1,
            1,
            null,
            CancellationToken.None));
    }

    [Fact]
    public async Task LastActiveAdminCannotBeDemoted()
    {
        var repo = new StubAdministrationRepository(singleAdmin: true);
        var service = new InternalUserAdministrationService(repo, new StubPasswordService());

        await Assert.ThrowsAsync<AppConflictException>(() => service.UpdateAsync(
            1,
            new UpdateInternalUserRequest("Admin", "NextTech", "admin@example.test", InternalRoles.Supervisor),
            1,
            null,
            CancellationToken.None));
    }

    [Fact]
    public async Task SearchRejectsExcessivePageSize()
    {
        var service = new InternalUserAdministrationService(new StubAdministrationRepository(), new StubPasswordService());

        await Assert.ThrowsAsync<AppValidationException>(() => service.SearchAsync(
            new InternalUserListRequest(PageSize: 101),
            CancellationToken.None));
    }

    private sealed class StubPasswordService : IPasswordService
    {
        public string Hash(string password) => "hashed:" + password;
        public PasswordCheckResult Verify(string hash, string providedPassword) => PasswordCheckResult.Success;
    }

    private sealed class StubAdministrationRepository : IInternalUserAdministrationRepository
    {
        private readonly Dictionary<int, InternalUserInfo> _users;
        private int _nextId = 3;

        public StubAdministrationRepository(bool singleAdmin = false)
        {
            _users = new Dictionary<int, InternalUserInfo>
            {
                [1] = User(1, "Admin", "NextTech", "admin@example.test", AdminRole(), true),
                [2] = User(2, "Delivery", "User", "delivery@example.test", DeliveryRole(), true)
            };

            if (!singleAdmin)
                _users[3] = User(3, "Second", "Admin", "admin2@example.test", AdminRole(), true);

            _nextId = _users.Keys.Max() + 1;
        }

        public bool UserStillExists(int id) => _users.ContainsKey(id);

        public Task<IReadOnlyList<InternalRoleInfo>> GetRolesAsync(CancellationToken ct)
            => Task.FromResult<IReadOnlyList<InternalRoleInfo>>([AdminRole(), SupervisorRole(), DeliveryRole()]);

        public Task<InternalRoleInfo?> FindRoleByCodeAsync(string roleCode, CancellationToken ct)
            => Task.FromResult<InternalRoleInfo?>(roleCode switch
            {
                InternalRoles.Admin => AdminRole(),
                InternalRoles.Supervisor => SupervisorRole(),
                InternalRoles.DeliveryDriver => DeliveryRole(),
                _ => null
            });

        public Task<InternalUserInfo?> FindByIdAsync(int userId, CancellationToken ct)
            => Task.FromResult(_users.GetValueOrDefault(userId));

        public Task<bool> EmailExistsAsync(string normalizedEmail, int? excludingUserId, CancellationToken ct)
            => Task.FromResult(_users.Values.Any(x =>
                x.Email == normalizedEmail && (!excludingUserId.HasValue || x.Id != excludingUserId.Value)));

        public Task<int> CountActiveAdminsAsync(CancellationToken ct)
            => Task.FromResult(_users.Values.Count(x => x.IsActive && x.Role.Code == InternalRoles.Admin));

        public Task<PagedResult<InternalUserInfo>> SearchAsync(InternalUserListRequest request, CancellationToken ct)
        {
            var items = _users.Values.OrderBy(x => x.Id).ToList();
            return Task.FromResult(new PagedResult<InternalUserInfo>(items, 1, 20, items.Count, 1));
        }

        public Task<InternalUserInfo> CreateAsync(CreateInternalUserData data, int actorUserId, string? ip, CancellationToken ct)
        {
            var role = data.RoleId switch
            {
                1 => AdminRole(),
                2 => SupervisorRole(),
                _ => DeliveryRole()
            };
            var id = _nextId++;
            var user = User(id, data.FirstName, data.LastName, data.Email, role, true) with { MustChangePassword = true };
            _users[id] = user;
            return Task.FromResult(user);
        }

        public Task<InternalUserInfo> UpdateAsync(int userId, UpdateInternalUserData data, int actorUserId, string? ip, CancellationToken ct)
        {
            var current = _users[userId];
            var role = data.RoleId switch
            {
                1 => AdminRole(),
                2 => SupervisorRole(),
                _ => DeliveryRole()
            };
            var updated = current with
            {
                FirstName = data.FirstName,
                LastName = data.LastName,
                FullName = data.FirstName + " " + data.LastName,
                Email = data.Email,
                Role = role
            };
            _users[userId] = updated;
            return Task.FromResult(updated);
        }

        public Task<InternalUserInfo> SetActiveAsync(int userId, bool active, int actorUserId, string? ip, CancellationToken ct)
        {
            var current = _users[userId];
            var updated = current with
            {
                IsActive = active,
                DeactivatedAtUtc = active ? null : DateTime.UtcNow
            };
            _users[userId] = updated;
            return Task.FromResult(updated);
        }

        public Task<InternalUserInfo> UnlockAsync(int userId, int actorUserId, string? ip, CancellationToken ct)
        {
            var updated = _users[userId] with { FailedAttempts = 0, LockedUntilUtc = null, IsLocked = false };
            _users[userId] = updated;
            return Task.FromResult(updated);
        }

        public Task<InternalUserInfo> ResetPasswordAsync(int userId, string passwordHash, int actorUserId, string? ip, CancellationToken ct)
        {
            var updated = _users[userId] with { MustChangePassword = true, FailedAttempts = 0, LockedUntilUtc = null, IsLocked = false };
            _users[userId] = updated;
            return Task.FromResult(updated);
        }

        private static InternalUserInfo User(
            int id,
            string firstName,
            string lastName,
            string email,
            InternalRoleInfo role,
            bool active)
            => new(
                id,
                firstName,
                lastName,
                firstName + " " + lastName,
                email,
                role,
                active,
                false,
                0,
                null,
                false,
                null,
                DateTime.UtcNow,
                null,
                null);

        private static InternalRoleInfo AdminRole() => new(1, InternalRoles.Admin, "Administrador");
        private static InternalRoleInfo SupervisorRole() => new(2, InternalRoles.Supervisor, "Supervisor");
        private static InternalRoleInfo DeliveryRole() => new(3, InternalRoles.DeliveryDriver, "Repartidor");
    }
}
