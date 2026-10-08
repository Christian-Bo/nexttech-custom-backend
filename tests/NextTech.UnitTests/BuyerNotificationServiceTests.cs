using NextTech.Application.Authentication;
using NextTech.Application.Credentials;
using NextTech.Application.Interfaces;
using NextTech.Application.Modules.Notifications;
using NextTech.Application.Notifications;

namespace NextTech.UnitTests;

public sealed class BuyerNotificationServiceTests
{
    [Theory]
    [InlineData(NotificationDeliveryChannels.Email, 1, 0)]
    [InlineData(NotificationDeliveryChannels.WhatsApp, 0, 1)]
    [InlineData(NotificationDeliveryChannels.EmailAndWhatsApp, 1, 1)]
    public async Task ResendWelcome_UsesRequestedChannel(
        string channel,
        int expectedEmailCalls,
        int expectedWhatsAppCalls)
    {
        var gateway = new StubGateway(ActiveBuyer());
        var email = new StubRegistrationNotificationSender();
        var whatsApp = new StubWhatsAppNotificationSender();
        var service = new BuyerNotificationService(gateway, email, whatsApp);

        var result = await service.ResendWelcomeAsync(
            21,
            new NotificationDeliveryRequest(channel),
            CancellationToken.None);

        Assert.Equal(channel, result.Channel);
        Assert.Equal(expectedEmailCalls, email.CallCount);
        Assert.Equal(expectedWhatsAppCalls, whatsApp.CallCount);
    }

    private static BuyerAuthRecord ActiveBuyer() => new(
        21,
        "buyer@example.test",
        "50254375269",
        null,
        "buyer",
        "hash",
        true,
        true,
        true,
        false,
        0,
        null,
        null);

    private sealed class StubRegistrationNotificationSender : IRegistrationNotificationSender
    {
        public int CallCount { get; private set; }

        public Task SendRegistrationWelcomeAsync(
            string email,
            string nickname,
            CancellationToken ct)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubWhatsAppNotificationSender : IWhatsAppNotificationSender
    {
        public int CallCount { get; private set; }

        public Task SendRegistrationWelcomeAsync(
            string phone,
            string email,
            string nickname,
            CancellationToken ct)
        {
            CallCount++;
            return Task.CompletedTask;
        }

        public Task SendCredentialAsync(
            string phone,
            string nickname,
            BuyerCredentialDocument document,
            CancellationToken ct)
            => Task.CompletedTask;
    }

    private sealed class StubGateway(BuyerAuthRecord buyer) : ICentralIdentityGateway
    {
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
}
