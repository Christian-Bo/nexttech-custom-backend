using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Interfaces;
using NextTech.Application.Modules.Profile;
using NextTech.Application.Profiles;
using NextTech.Infrastructure.Authentication;

namespace NextTech.UnitTests;

public sealed class BuyerProfileServiceTests
{
    [Fact]
    public async Task Get_ReturnsFriendlyRoleNotificationAndPhotoState()
    {
        var passwords = new PasswordService();
        var gateway = new StubGateway(ActiveBuyer(passwords.Hash("Current123")));
        var repository = new StubProfileRepository(ProfileData());
        var service = new BuyerProfileService(repository, gateway, passwords, new StubTokenService());

        var result = await service.GetAsync(46, CancellationToken.None);

        Assert.Equal("COMPRADOR", result.Role.Code);
        Assert.Equal("Comprador", result.Role.Name);
        Assert.Equal("EMAIL_AND_WHATSAPP", result.NotificationPreference.Code);
        Assert.Equal("Correo electrónico y WhatsApp", result.NotificationPreference.Name);
        Assert.True(result.Photo.HasFaceEnrollment);
        Assert.Equal("image/png", result.Photo.DisplayPhotoContentType);
    }

    [Fact]
    public async Task Update_MapsNotificationCodeAndReturnsRefreshedProfile()
    {
        var passwords = new PasswordService();
        var gateway = new StubGateway(ActiveBuyer(passwords.Hash("Current123")));
        var repository = new StubProfileRepository(ProfileData());
        var service = new BuyerProfileService(repository, gateway, passwords, new StubTokenService());

        var result = await service.UpdateAsync(
            46,
            new UpdateBuyerProfileRequest(
                "+502 5555-0101",
                "buyer-updated",
                new DateTime(2000, 5, 1),
                "WHATSAPP"),
            CancellationToken.None);

        Assert.Equal("buyer-updated", result.Profile.Nickname);
        Assert.Equal("+502 5555-0101", result.Profile.Phone);
        Assert.Equal("WHATSAPP", result.Profile.NotificationPreference.Code);
        Assert.False(repository.LastUpdate!.NotifyByEmail);
        Assert.True(repository.LastUpdate.NotifyByWhatsApp);
        Assert.Equal(ActorTypes.Buyer, result.RefreshedToken.ActorType);
    }

    [Fact]
    public async Task Update_RejectsIdentifierOwnedByAnotherBuyer()
    {
        var passwords = new PasswordService();
        var gateway = new StubGateway(ActiveBuyer(passwords.Hash("Current123")));
        var repository = new StubProfileRepository(ProfileData());
        repository.IdentifiersInUse.Add("nickname-taken");
        var service = new BuyerProfileService(repository, gateway, passwords, new StubTokenService());

        await Assert.ThrowsAsync<AppConflictException>(() => service.UpdateAsync(
            46,
            new UpdateBuyerProfileRequest("55550101", "nickname-taken", null, "EMAIL"),
            CancellationToken.None));

        Assert.Null(repository.LastUpdate);
    }

    [Fact]
    public async Task Update_RejectsFutureBirthDate()
    {
        var passwords = new PasswordService();
        var gateway = new StubGateway(ActiveBuyer(passwords.Hash("Current123")));
        var repository = new StubProfileRepository(ProfileData());
        var service = new BuyerProfileService(repository, gateway, passwords, new StubTokenService());

        await Assert.ThrowsAsync<AppValidationException>(() => service.UpdateAsync(
            46,
            new UpdateBuyerProfileRequest(
                "55550101",
                "buyer",
                DateTime.UtcNow.Date.AddDays(1),
                "EMAIL"),
            CancellationToken.None));
    }

    [Fact]
    public async Task ChangePassword_RequiresCurrentPasswordAndPersistsNewHash()
    {
        var passwords = new PasswordService();
        var originalHash = passwords.Hash("Current123");
        var gateway = new StubGateway(ActiveBuyer(originalHash));
        var repository = new StubProfileRepository(ProfileData());
        var service = new BuyerProfileService(repository, gateway, passwords, new StubTokenService());

        var result = await service.ChangePasswordAsync(
            46,
            new ChangeBuyerPasswordRequest("Current123", "NewPassword456"),
            CancellationToken.None);

        Assert.NotNull(gateway.LastPasswordHash);
        Assert.Equal(PasswordCheckResult.Success, passwords.Verify(gateway.LastPasswordHash!, "NewPassword456"));
        Assert.Equal(ActorTypes.Buyer, result.RefreshedToken.ActorType);
        Assert.False(gateway.LastUnlockValue);
    }

    [Fact]
    public async Task ChangePassword_RejectsWrongCurrentPassword()
    {
        var passwords = new PasswordService();
        var gateway = new StubGateway(ActiveBuyer(passwords.Hash("Current123")));
        var repository = new StubProfileRepository(ProfileData());
        var service = new BuyerProfileService(repository, gateway, passwords, new StubTokenService());

        await Assert.ThrowsAsync<AppUnauthorizedException>(() => service.ChangePasswordAsync(
            46,
            new ChangeBuyerPasswordRequest("Wrong123", "NewPassword456"),
            CancellationToken.None));

        Assert.Null(gateway.LastPasswordHash);
    }

    [Fact]
    public async Task GetDisplayPhoto_RejectsMissingPhoto()
    {
        var passwords = new PasswordService();
        var gateway = new StubGateway(ActiveBuyer(passwords.Hash("Current123")));
        var repository = new StubProfileRepository(ProfileData(), photo: null);
        var service = new BuyerProfileService(repository, gateway, passwords, new StubTokenService());

        await Assert.ThrowsAsync<AppNotFoundException>(() =>
            service.GetDisplayPhotoAsync(46, CancellationToken.None));
    }

    private static BuyerAuthRecord ActiveBuyer(
        string passwordHash,
        long id = 46,
        string nickname = "buyer")
        => new(
            id,
            $"buyer{id}@example.test",
            "55550100",
            new DateTime(2000, 1, 1),
            nickname,
            passwordHash,
            true,
            true,
            true,
            false,
            0,
            4,
            5);

    private static BuyerProfileData ProfileData()
        => new(
            46,
            "buyer46@example.test",
            "55550100",
            new DateTime(2000, 1, 1),
            "buyer",
            true,
            true,
            true,
            false,
            true,
            true,
            "image/png",
            DateTimeOffset.UtcNow.AddHours(-1),
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddDays(-20),
            DateTimeOffset.UtcNow.AddMinutes(-5));

    private sealed class StubProfileRepository(
        BuyerProfileData data,
        BuyerDisplayPhoto? photo = null) : IBuyerProfileRepository
    {
        private BuyerProfileData _data = data;
        private readonly BuyerDisplayPhoto? _photo = photo;

        public BuyerProfileUpdateData? LastUpdate { get; private set; }
        public HashSet<string> IdentifiersInUse { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<BuyerProfileData?> GetAsync(long buyerId, CancellationToken ct)
            => Task.FromResult<BuyerProfileData?>(buyerId == _data.BuyerId ? _data : null);

        public Task<bool> IsIdentifierInUseByOtherAsync(
            string identifier,
            long buyerId,
            CancellationToken ct)
            => Task.FromResult(IdentifiersInUse.Contains(identifier));

        public Task UpdateAsync(long buyerId, BuyerProfileUpdateData value, CancellationToken ct)
        {
            LastUpdate = value;
            _data = _data with
            {
                Phone = value.Phone,
                Nickname = value.Nickname,
                BirthDate = value.BirthDate,
                NotifyByEmail = value.NotifyByEmail,
                NotifyByWhatsApp = value.NotifyByWhatsApp,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
            return Task.CompletedTask;
        }

        public Task<BuyerDisplayPhoto?> GetDisplayPhotoAsync(long buyerId, CancellationToken ct)
            => Task.FromResult(buyerId == _data.BuyerId ? _photo : null);
    }

    private sealed class StubTokenService : ITokenService
    {
        public AccessTokenResult CreateBuyerToken(BuyerProfile buyer)
            => new("buyer-token", DateTime.UtcNow.AddHours(1), ActorTypes.Buyer);

        public AccessTokenResult CreateInternalToken(InternalUserAuthRecord user)
            => throw new NotSupportedException();
    }

    private sealed class StubGateway(BuyerAuthRecord buyer) : ICentralIdentityGateway
    {
        private BuyerAuthRecord _buyer = buyer;

        public string? LastPasswordHash { get; private set; }
        public bool LastUnlockValue { get; private set; }

        public Task<BuyerAuthRecord?> FindByIdentifierAsync(string identifier, CancellationToken ct)
        {
            if (string.Equals(identifier, _buyer.Correo, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(identifier, _buyer.Nickname, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(identifier, _buyer.Telefono, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult<BuyerAuthRecord?>(_buyer);
            }

            return Task.FromResult<BuyerAuthRecord?>(null);
        }

        public Task<BuyerAuthRecord?> FindByIdAsync(long idUsuario, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(idUsuario == _buyer.IdUsuario ? _buyer : null);

        public Task<BuyerAuthRecord?> FindByQrHashAsync(string qrHash, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(null);

        public Task<long> RegisterAsync(BuyerRegistrationData data, CancellationToken ct)
            => throw new NotSupportedException();

        public Task SetPasswordHashAsync(long idUsuario, string passwordHash, bool unlock, CancellationToken ct)
        {
            LastPasswordHash = passwordHash;
            LastUnlockValue = unlock;
            _buyer = _buyer with { PasswordHash = passwordHash };
            return Task.CompletedTask;
        }

        public Task RegisterFailedLoginAsync(
            long? idUsuario,
            string identifier,
            string method,
            string reason,
            CancellationToken ct)
            => Task.CompletedTask;

        public Task RegisterSuccessfulLoginAsync(
            long idUsuario,
            string identifier,
            string method,
            CancellationToken ct)
            => Task.CompletedTask;

        public Task<bool> VerifyLegacyPasswordAsync(string identifier, string password, CancellationToken ct)
            => Task.FromResult(false);

        public Task SetQrHashAsync(long idUsuario, string qrHash, CancellationToken ct)
            => throw new NotSupportedException();

        public Task CreateRecoveryTokenAsync(
            long idUsuario,
            string tokenHash,
            DateTimeOffset expiresAt,
            CancellationToken ct)
            => throw new NotSupportedException();

        public Task<long?> FindValidRecoveryUserAsync(string tokenHash, CancellationToken ct)
            => Task.FromResult<long?>(null);

        public Task ConsumeRecoveryTokenAsync(string tokenHash, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
