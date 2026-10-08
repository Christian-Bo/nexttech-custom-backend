using System.Security.Cryptography;
using System.Text;
using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Credentials;
using NextTech.Application.Face;
using NextTech.Application.Interfaces;
using NextTech.Application.Modules.Credentials;
using NextTech.Application.Notifications;

namespace NextTech.UnitTests;

public sealed class BuyerCredentialServiceTests
{
    [Fact]
    public async Task Issue_RotatesQrAfterPdfGenerationAndSendsDocument()
    {
        var gateway = new StubGateway(ActiveBuyer());
        var store = new StubEnrollmentStore(Enrollment());
        var generator = new StubPdfGenerator();
        var notifications = new StubNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = new BuyerCredentialService(gateway, store, generator, notifications, whatsApp);

        var result = await service.IssueAsync(21, CancellationToken.None);

        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal("nexttech-credential-21.pdf", result.FileName);
        Assert.NotNull(generator.Data);
        Assert.Equal("COMPRADOR", generator.Data!.Role);
        Assert.Equal("buyer", generator.Data.Nickname);
        Assert.Equal(new byte[] { 1, 2, 3 }, generator.Data.PortraitContent);

        var expectedHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(generator.Data.QrCredential)))
            .ToLowerInvariant();

        Assert.Equal(expectedHash, gateway.LastQrHash);
        Assert.NotEqual(generator.Data.QrCredential, gateway.LastQrHash);
        Assert.Same(result, notifications.Document);
        Assert.Null(whatsApp.Document);
    }

    [Fact]
    public async Task Issue_SendsPdfByWhatsAppWhenPreferenceIsEnabled()
    {
        var gateway = new StubGateway(ActiveBuyer(
            notifyByEmail: false,
            notifyByWhatsApp: true,
            phone: "50254375269"));
        var store = new StubEnrollmentStore(Enrollment());
        var generator = new StubPdfGenerator();
        var notifications = new StubNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = new BuyerCredentialService(gateway, store, generator, notifications, whatsApp);

        var result = await service.IssueAsync(21, CancellationToken.None);

        Assert.Null(notifications.Document);
        Assert.Same(result, whatsApp.Document);
        Assert.Equal("50254375269", whatsApp.Phone);
        Assert.Equal("buyer", whatsApp.Nickname);
    }

    [Fact]
    public async Task Reissue_WithWhatsAppChannel_OverridesStoredEmailPreferenceAndRotatesQr()
    {
        var gateway = new StubGateway(ActiveBuyer(
            notifyByEmail: true,
            notifyByWhatsApp: false,
            phone: "50254375269"));
        var store = new StubEnrollmentStore(Enrollment());
        var generator = new StubPdfGenerator();
        var notifications = new StubNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = new BuyerCredentialService(gateway, store, generator, notifications, whatsApp);

        var result = await service.ReissueAsync(
            21,
            new NotificationDeliveryRequest(NotificationDeliveryChannels.WhatsApp),
            CancellationToken.None);

        Assert.NotNull(gateway.LastQrHash);
        Assert.Null(notifications.Document);
        Assert.Same(result, whatsApp.Document);
    }

    [Fact]
    public async Task Reissue_WithBothChannels_SendsSameDocumentThroughBothChannels()
    {
        var gateway = new StubGateway(ActiveBuyer(
            notifyByEmail: false,
            notifyByWhatsApp: false,
            phone: "50254375269"));
        var store = new StubEnrollmentStore(Enrollment());
        var generator = new StubPdfGenerator();
        var notifications = new StubNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = new BuyerCredentialService(gateway, store, generator, notifications, whatsApp);

        var result = await service.ReissueAsync(
            21,
            new NotificationDeliveryRequest(NotificationDeliveryChannels.EmailAndWhatsApp),
            CancellationToken.None);

        Assert.Same(result, notifications.Document);
        Assert.Same(result, whatsApp.Document);
    }

    [Fact]
    public async Task Issue_RejectsBuyerWithoutActiveFacialEnrollment()
    {
        var gateway = new StubGateway(ActiveBuyer());
        var store = new StubEnrollmentStore(null);
        var generator = new StubPdfGenerator();
        var notifications = new StubNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = new BuyerCredentialService(gateway, store, generator, notifications, whatsApp);

        await Assert.ThrowsAsync<AppConflictException>(() =>
            service.IssueAsync(21, CancellationToken.None));

        Assert.Null(gateway.LastQrHash);
        Assert.Null(generator.Data);
        Assert.Null(notifications.Document);
    }

    [Fact]
    public async Task Issue_DoesNotRotateQrWhenPdfGenerationFails()
    {
        var gateway = new StubGateway(ActiveBuyer());
        var store = new StubEnrollmentStore(Enrollment());
        var generator = new StubPdfGenerator(throwOnGenerate: true);
        var notifications = new StubNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = new BuyerCredentialService(gateway, store, generator, notifications, whatsApp);

        await Assert.ThrowsAsync<AppDependencyException>(() =>
            service.IssueAsync(21, CancellationToken.None));

        Assert.Null(gateway.LastQrHash);
        Assert.Null(notifications.Document);
    }

    private static BuyerAuthRecord ActiveBuyer(
        bool notifyByEmail = true,
        bool notifyByWhatsApp = false,
        string? phone = "50254375269") => new(
        21,
        "buyer@example.test",
        phone,
        null,
        "buyer",
        "hash",
        notifyByEmail,
        notifyByWhatsApp,
        true,
        false,
        0,
        null,
        null);

    private static BuyerFaceEnrollment Enrollment() => new(
        21,
        new FaceImage([0xFF, 0xD8, 0xFF, 0xE0, 1], "image/jpeg", "reference.jpg"),
        [1, 2, 3],
        "image/png",
        DateTimeOffset.UtcNow);

    private sealed class StubPdfGenerator(bool throwOnGenerate = false) : IBuyerCredentialPdfGenerator
    {
        public BuyerCredentialPdfData? Data { get; private set; }

        public BuyerCredentialDocument Generate(BuyerCredentialPdfData data)
        {
            Data = data;
            if (throwOnGenerate)
                throw new InvalidOperationException("pdf failure");

            return new BuyerCredentialDocument(
                [1, 2, 3, 4],
                "application/pdf",
                $"nexttech-credential-{data.BuyerId}.pdf",
                data.IssuedAtUtc);
        }
    }

    private sealed class StubNotificationSender : IBuyerCredentialNotificationSender
    {
        public BuyerCredentialDocument? Document { get; private set; }

        public Task SendCredentialAsync(
            string email,
            string nickname,
            BuyerCredentialDocument document,
            CancellationToken ct)
        {
            Document = document;
            return Task.CompletedTask;
        }
    }

    private sealed class StubWhatsAppNotificationSender : IWhatsAppNotificationSender
    {
        public string? Phone { get; private set; }
        public string? Nickname { get; private set; }
        public BuyerCredentialDocument? Document { get; private set; }

        public Task SendRegistrationWelcomeAsync(
            string phone,
            string email,
            string nickname,
            CancellationToken ct)
            => Task.CompletedTask;

        public Task SendCredentialAsync(
            string phone,
            string nickname,
            BuyerCredentialDocument document,
            CancellationToken ct)
        {
            Phone = phone;
            Nickname = nickname;
            Document = document;
            return Task.CompletedTask;
        }
    }

    private sealed class StubEnrollmentStore(BuyerFaceEnrollment? enrollment) : IBuyerFaceEnrollmentStore
    {
        public Task<BuyerFaceEnrollment?> GetActiveAsync(long buyerId, CancellationToken ct)
            => Task.FromResult(enrollment?.BuyerId == buyerId ? enrollment : null);

        public Task<BuyerFaceEnrollment> UpsertAsync(
            BuyerFaceEnrollment value,
            CancellationToken ct)
            => throw new NotSupportedException();
    }

    private sealed class StubGateway(BuyerAuthRecord buyer) : ICentralIdentityGateway
    {
        public string? LastQrHash { get; private set; }

        public Task<BuyerAuthRecord?> FindByIdentifierAsync(string identifier, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(buyer);

        public Task<BuyerAuthRecord?> FindByIdAsync(long idUsuario, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(idUsuario == buyer.IdUsuario ? buyer : null);

        public Task<BuyerAuthRecord?> FindByQrHashAsync(string qrHash, CancellationToken ct)
            => Task.FromResult<BuyerAuthRecord?>(null);

        public Task<long> RegisterAsync(BuyerRegistrationData data, CancellationToken ct)
            => throw new NotSupportedException();

        public Task SetPasswordHashAsync(long idUsuario, string passwordHash, bool unlock, CancellationToken ct)
            => throw new NotSupportedException();

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
        {
            LastQrHash = qrHash;
            return Task.CompletedTask;
        }

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
