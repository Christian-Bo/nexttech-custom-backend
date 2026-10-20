using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NextTech.Application.Credentials;
using NextTech.Infrastructure.WhatsApp;

namespace NextTech.UnitTests;

public sealed class WhatsAppNotificationSenderTests
{
    [Fact]
    public async Task RegistrationWelcome_UsesTextEndpointAndContainsFriendlyAccountSummary()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-whatsapp.example/")
        };
        var sender = CreateSender(client, enabled: true);

        await sender.SendRegistrationWelcomeAsync(
            "50254375269",
            "buyer@example.test",
            "buyer",
            CancellationToken.None);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("/messages/text", handler.RequestUri?.AbsolutePath);

        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("50254375269", json.RootElement.GetProperty("to").GetString());
        var message = json.RootElement.GetProperty("message").GetString();
        Assert.Contains("Bienvenido", message);
        Assert.Contains("buyer@example.test", message);
        Assert.Contains("buyer", message);
        Assert.Matches(@"nunca (envía|enviará) tu contraseña", message);
    }

    [Theory]
    [InlineData("55550101")]
    [InlineData("50255550101")]
    [InlineData("+502 5555-0101")]
    public async Task RegistrationWelcome_NormalizesGuatemalaNumberAtWhatsAppBoundary(string enteredPhone)
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-whatsapp.example/")
        };
        var sender = CreateSender(client, enabled: true);

        await sender.SendRegistrationWelcomeAsync(
            enteredPhone, "buyer@example.test", "buyer", CancellationToken.None);

        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("50255550101", json.RootElement.GetProperty("to").GetString());
        Assert.Equal("/messages/text", handler.RequestUri?.AbsolutePath);
    }

    [Theory]
    [InlineData("55550101")]
    [InlineData("+502 5555-0101")]
    public async Task CredentialDocument_NormalizesPhoneInMediaPayload(string enteredPhone)
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-whatsapp.example/")
        };
        var sender = CreateSender(client, enabled: true);
        var credential = new BuyerCredentialDocument(
            [1, 2, 3], "application/pdf", "credential.pdf", DateTimeOffset.UtcNow);

        await sender.SendCredentialAsync(enteredPhone, "buyer", credential, CancellationToken.None);

        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("50255550101", json.RootElement.GetProperty("to").GetString());
        Assert.Equal("/messages/media", handler.RequestUri?.AbsolutePath);
    }

    [Fact]
    public async Task OrderNotifications_NormalizeLegacyBuyerPhone()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-whatsapp.example/")
        };
        var sender = CreateSender(client, enabled: true);

        var sent = await sender.SendOrderReadyAsync(
            "55550101", "buyer", "NT-001", CancellationToken.None);

        Assert.True(sent);
        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("50255550101", json.RootElement.GetProperty("to").GetString());
    }

    [Theory]
    [InlineData("5555010")]
    [InlineData("50355550101")]
    [InlineData("55550101x")]
    public async Task InvalidPhone_DoesNotCallWhatsAppProvider(string enteredPhone)
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-whatsapp.example/")
        };
        var sender = CreateSender(client, enabled: true);

        var sent = await sender.SendOrderReadyAsync(
            enteredPhone, "buyer", "NT-001", CancellationToken.None);

        Assert.False(sent);
        Assert.Null(handler.RequestUri);
    }

    [Fact]
    public async Task CredentialDocument_UsesMediaEndpointAndBase64Payload()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-whatsapp.example/")
        };
        var sender = CreateSender(client, enabled: true);
        var document = new BuyerCredentialDocument(
            [1, 2, 3, 4],
            "application/pdf",
            "credential.pdf",
            DateTimeOffset.UtcNow);

        await sender.SendCredentialAsync(
            "50254375269",
            "buyer",
            document,
            CancellationToken.None);

        Assert.Equal("/messages/media", handler.RequestUri?.AbsolutePath);

        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("50254375269", json.RootElement.GetProperty("to").GetString());
        Assert.Equal("credential.pdf", json.RootElement.GetProperty("filename").GetString());
        Assert.Equal("application/pdf", json.RootElement.GetProperty("mimeType").GetString());
        Assert.Equal(Convert.ToBase64String(document.Content), json.RootElement.GetProperty("base64Data").GetString());
        Assert.Contains("credencial digital está lista", json.RootElement.GetProperty("caption").GetString());
    }

    [Fact]
    public async Task PurchaseConfirmation_UsesMediaEndpointWithReceiptAndOrderSummary()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-whatsapp.example/")
        };
        var sender = CreateSender(client, enabled: true);

        var sent = await sender.SendPurchaseConfirmationAsync(
            "50254375269",
            "buyer",
            "NT-TEST-001",
            1249.00m,
            "Pagado",
            [1, 2, 3, 4],
            CancellationToken.None);

        Assert.True(sent);
        Assert.Equal("/messages/media", handler.RequestUri?.AbsolutePath);

        using var json = JsonDocument.Parse(handler.Body!);
        Assert.Equal("constancia-NT-TEST-001.pdf", json.RootElement.GetProperty("filename").GetString());
        var caption = json.RootElement.GetProperty("caption").GetString();
        Assert.Contains("Compra confirmada", caption);
        Assert.Contains("NT-TEST-001", caption);
        Assert.Contains("Pagado", caption);
    }

    [Fact]
    public async Task PasswordRecovery_UsesConfiguredResetUrlWithoutExposingTokenSeparately()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-whatsapp.example/")
        };
        var sender = CreateSender(client, enabled: true);

        await sender.SendPasswordRecoveryAsync(
            "50254375269",
            "buyer",
            "token-test",
            DateTimeOffset.UtcNow.AddMinutes(30),
            CancellationToken.None);

        Assert.Equal("/messages/text", handler.RequestUri?.AbsolutePath);
        using var json = JsonDocument.Parse(handler.Body!);
        var message = json.RootElement.GetProperty("message").GetString();
        Assert.Contains("https://app.example/reset-password?token=token-test", message);
        Assert.Contains("Recuperación de acceso", message);
    }

    [Fact]
    public async Task DisabledIntegration_DoesNotCallProvider()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://api-whatsapp.example/")
        };
        var sender = CreateSender(client, enabled: false);

        await sender.SendRegistrationWelcomeAsync(
            "50254375269",
            "buyer@example.test",
            "buyer",
            CancellationToken.None);

        Assert.Null(handler.RequestUri);
    }

    private static WhatsAppNotificationSender CreateSender(HttpClient client, bool enabled)
        => new(
            client,
            Options.Create(new WhatsAppOptions
            {
                Enabled = enabled,
                BaseUrl = "https://api-whatsapp.example",
                ApiKey = "test-key",
                ApiKeyHeader = "X-API-Key",
                TimeoutSeconds = 30
            }),
            Options.Create(new NextTech.Infrastructure.Email.SmtpOptions
            {
                RecoveryUrlBase = "https://app.example/reset-password"
            }),
            NullLogger<WhatsAppNotificationSender>.Instance);

    private sealed class RecordingHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? Body { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;
            Body = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(statusCode);
        }
    }
}
