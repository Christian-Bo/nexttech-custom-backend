using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Credentials;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;
using NextTech.Application.Modules.Auth;

namespace NextTech.UnitTests;

public sealed class BuyerAuthServiceNotificationTests
{
    [Fact]
    public async Task Register_WithWhatsAppOnly_SendsWelcomeThroughWhatsAppAndSkipsEmail()
    {
        var buyer = ActiveBuyer(notifyByEmail: false, notifyByWhatsApp: true);
        var gateway = new StubGateway(buyer);
        var email = new StubRegistrationNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = CreateService(gateway, email, whatsApp);

        var result = await service.RegisterAsync(new BuyerRegisterRequest(
            "buyer@example.test",
            "50254375269",
            "buyer",
            "Password1",
            null,
            NotifyByEmail: false,
            NotifyByWhatsApp: true), CancellationToken.None);

        Assert.Equal(77, result.BuyerId);
        Assert.Equal(0, email.CallCount);
        Assert.Equal(1, whatsApp.RegistrationCallCount);
        Assert.Equal("50254375269", whatsApp.Phone);
        Assert.Equal("buyer@example.test", whatsApp.Email);
        Assert.Equal("buyer", whatsApp.Nickname);
        Assert.NotNull(gateway.RegistrationData);
        Assert.True(gateway.RegistrationData!.NotificaWhatsApp);
        Assert.False(gateway.RegistrationData.NotificaEmail);
    }

    [Fact]
    public async Task Register_WithEmailAndWhatsApp_SendsWelcomeThroughBothChannels()
    {
        var buyer = ActiveBuyer(notifyByEmail: true, notifyByWhatsApp: true);
        var gateway = new StubGateway(buyer);
        var email = new StubRegistrationNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = CreateService(gateway, email, whatsApp);

        await service.RegisterAsync(new BuyerRegisterRequest(
            "buyer@example.test",
            "50254375269",
            "buyer",
            "Password1",
            null,
            NotifyByEmail: true,
            NotifyByWhatsApp: true), CancellationToken.None);

        Assert.Equal(1, email.CallCount);
        Assert.Equal("buyer@example.test", email.Email);
        Assert.Equal("buyer", email.Nickname);
        Assert.Equal(1, whatsApp.RegistrationCallCount);
    }

    [Fact]
    public async Task Register_WithInvalidWhatsAppPhone_IsRejectedBeforePersistence()
    {
        var buyer = ActiveBuyer(notifyByEmail: false, notifyByWhatsApp: true);
        var gateway = new StubGateway(buyer);
        var email = new StubRegistrationNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = CreateService(gateway, email, whatsApp);

        await Assert.ThrowsAsync<AppValidationException>(() => service.RegisterAsync(new BuyerRegisterRequest(
            "buyer@example.test",
            "123",
            "buyer",
            "Password1",
            null,
            NotifyByEmail: false,
            NotifyByWhatsApp: true), CancellationToken.None));

        Assert.Null(gateway.RegistrationData);
        Assert.Equal(0, whatsApp.RegistrationCallCount);
    }

    private static BuyerAuthService CreateService(
        ICentralIdentityGateway gateway,
        IRegistrationNotificationSender email,
        IWhatsAppNotificationSender whatsApp)
        => new(
            gateway,
            new StubPasswordService(),
            new StubTokenService(),
            new UnusedFaceBiometricService(),
            new UnusedEnrollmentStore(),
            email,
            whatsApp,
            new StubRecoveryNotificationSender());

    private static BuyerAuthRecord ActiveBuyer(bool notifyByEmail, bool notifyByWhatsApp) => new(
        77,
        "buyer@example.test",
        "50254375269",
        null,
        "buyer",
        "hashed",
        notifyByEmail,
        notifyByWhatsApp,
        true,
        false,
        0,
        null,
        null);

    private sealed class StubGateway(BuyerAuthRecord buyer) : ICentralIdentityGateway
    {
        public BuyerRegistrationData? RegistrationData { get; private set; }

        public Task<BuyerAuthRecord?> FindByIdentifierAsync(string identifier, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(null);

        public Task<BuyerAuthRecord?> FindByIdAsync(long idUsuario, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(idUsuario == buyer.IdUsuario ? buyer : null);

        public Task<BuyerAuthRecord?> FindByQrHashAsync(string qrHash, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(null);

        public Task<long> RegisterAsync(BuyerRegistrationData data, CancellationToken ct)
        {
            RegistrationData = data;
            return Task.FromResult(buyer.IdUsuario);
        }

        public Task SetPasswordHashAsync(long idUsuario, string passwordHash, bool unlock, CancellationToken ct)
            => throw new NotSupportedException();

        public Task RegisterFailedLoginAsync(long? idUsuario, string identifier, string method, string reason, CancellationToken ct)
            => Task.CompletedTask;

        public Task RegisterSuccessfulLoginAsync(long idUsuario, string identifier, string method, CancellationToken ct)
            => Task.CompletedTask;

        public Task<bool> VerifyLegacyPasswordAsync(string identifier, string password, CancellationToken ct)
            => Task.FromResult(false);

        public Task SetQrHashAsync(long idUsuario, string qrHash, CancellationToken ct)
            => throw new NotSupportedException();

        public Task CreateRecoveryTokenAsync(long idUsuario, string tokenHash, DateTimeOffset expiresAt, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<long?> FindValidRecoveryUserAsync(string tokenHash, CancellationToken ct)
            => Task.FromResult<long?>(null);

        public Task ConsumeRecoveryTokenAsync(string tokenHash, CancellationToken ct)
            => throw new NotSupportedException();
    }

    private sealed class StubPasswordService : IPasswordService
    {
        public string Hash(string password) => "hashed";
        public PasswordCheckResult Verify(string hash, string providedPassword) => PasswordCheckResult.Failed;
    }

    private sealed class StubTokenService : ITokenService
    {
        public AccessTokenResult CreateBuyerToken(BuyerProfile buyer)
            => new("token", DateTime.UtcNow.AddHours(1), ActorTypes.Buyer);

        public AccessTokenResult CreateInternalToken(InternalUserAuthRecord user)
            => throw new NotSupportedException();
    }

    private sealed class StubRegistrationNotificationSender : IRegistrationNotificationSender
    {
        public int CallCount { get; private set; }
        public string? Email { get; private set; }
        public string? Nickname { get; private set; }

        public Task SendRegistrationWelcomeAsync(
            string email,
            string nickname,
            CancellationToken ct)
        {
            CallCount++;
            Email = email;
            Nickname = nickname;
            return Task.CompletedTask;
        }
    }

    private sealed class StubWhatsAppNotificationSender : IWhatsAppNotificationSender
    {
        public int RegistrationCallCount { get; private set; }
        public string? Phone { get; private set; }
        public string? Email { get; private set; }
        public string? Nickname { get; private set; }

        public Task SendRegistrationWelcomeAsync(
            string phone,
            string email,
            string nickname,
            CancellationToken ct)
        {
            RegistrationCallCount++;
            Phone = phone;
            Email = email;
            Nickname = nickname;
            return Task.CompletedTask;
        }

        public Task SendCredentialAsync(
            string phone,
            string nickname,
            BuyerCredentialDocument document,
            CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class StubRecoveryNotificationSender : IRecoveryNotificationSender
    {
        public Task SendPasswordRecoveryAsync(
            string email,
            string rawToken,
            DateTimeOffset expiresAt,
            CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class UnusedFaceBiometricService : IFaceBiometricService
    {
        public Task<FaceChallengeResult> CreateLivenessChallengeAsync(CancellationToken ct)
            => throw new NotSupportedException();

        public Task<ProtectedFaceEnrollmentResult> CreateTemplateAsync(FaceImage image, CancellationToken ct)
            => throw new NotSupportedException();

        public Task<FaceVerificationResult> VerifyTemplateLiveAsync(
            string biometricTemplate,
            string challengeId,
            FaceImage neutralImage,
            FaceImage challengeImage,
            CancellationToken ct)
            => throw new NotSupportedException();

        public Task<FaceSegmentationResult> SegmentForCardAsync(FaceImage image, CancellationToken ct)
            => throw new NotSupportedException();
    }

    private sealed class UnusedEnrollmentStore : IBuyerFaceEnrollmentStore
    {
        public Task<BuyerFaceEnrollment?> GetActiveAsync(long buyerId, CancellationToken ct)
            => Task.FromResult<BuyerFaceEnrollment?>(null);

        public Task<BuyerFaceEnrollment> UpsertAsync(BuyerFaceEnrollment enrollment, CancellationToken ct)
            => throw new NotSupportedException();
    }
}
