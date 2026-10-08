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
        Assert.Contains("nunca envía tu contraseña", message);
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
