using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NextTech.Application.Common;
using NextTech.Application.Payments;

namespace NextTech.Infrastructure.Payments;

public sealed class RecurrenteCheckoutClient(
    HttpClient http,
    IOptions<RecurrenteOptions> options,
    ILogger<RecurrenteCheckoutClient> logger) : IRecurrenteCheckoutClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly RecurrenteOptions _options = options.Value;

    public bool Enabled => _options.Enabled;

    public async Task<RecurrenteCheckoutSession> CreateCheckoutAsync(
        RecurrenteCheckoutCreateRequest request,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();

        using var message = new HttpRequestMessage(HttpMethod.Post, "api/checkouts")
        {
            Content = JsonContent.Create(
                new RecurrenteCreateBody(
                    [
                        new RecurrenteItem(
                            request.ItemName,
                            request.AmountInCents,
                            "GTQ",
                            1)
                    ],
                    _options.SuccessUrl,
                    _options.CancelUrl,
                    request.CustomerEmail,
                    request.Metadata),
                options: Json)
        };
        AddKeys(message);

        using var response = await http.SendAsync(message, cancellationToken);
        var session = await ReadSessionAsync(response, "crear el checkout de Recurrente", cancellationToken);
        if (string.IsNullOrWhiteSpace(session.CheckoutUrl))
        {
            throw new AppDependencyException("Recurrente no devolvió la URL de pago.");
        }

        return session;
    }

    public async Task<RecurrenteCheckoutSession> GetCheckoutAsync(
        string checkoutId,
        CancellationToken cancellationToken)
    {
        EnsureConfigured();
        if (string.IsNullOrWhiteSpace(checkoutId))
        {
            throw new AppValidationException("El identificador de checkout es obligatorio.");
        }

        using var message = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/checkouts/{Uri.EscapeDataString(checkoutId.Trim())}");
        AddKeys(message);

        using var response = await http.SendAsync(message, cancellationToken);
        return await ReadSessionAsync(response, "consultar el checkout de Recurrente", cancellationToken);
    }

    private void EnsureConfigured()
    {
        if (!_options.Enabled)
        {
            throw new AppValidationException("El pago con tarjeta no está habilitado.");
        }

        if (string.IsNullOrWhiteSpace(_options.SecretKey) ||
            string.IsNullOrWhiteSpace(_options.SuccessUrl) ||
            string.IsNullOrWhiteSpace(_options.CancelUrl))
        {
            throw new AppDependencyException("Falta la configuración de Recurrente.");
        }
    }

    private void AddKeys(HttpRequestMessage message)
    {
        message.Headers.TryAddWithoutValidation("X-SECRET-KEY", _options.SecretKey);
    }

    private async Task<RecurrenteCheckoutSession> ReadSessionAsync(
        HttpResponseMessage response,
        string operacion,
        CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Recurrente respondió {Status} al {Operacion}.",
                (int)response.StatusCode,
                operacion);
            throw new AppDependencyException("Recurrente no pudo procesar el pago con tarjeta.");
        }

        var session = RecurrenteCheckoutParser.TryParse(json);
        if (session is null)
        {
            throw new AppDependencyException("Recurrente devolvió una respuesta inválida.");
        }

        return session;
    }

    private sealed record RecurrenteCreateBody(
        IReadOnlyList<RecurrenteItem> Items,
        string SuccessUrl,
        string CancelUrl,
        string CustomerEmail,
        IReadOnlyDictionary<string, string> Metadata);

    private sealed record RecurrenteItem(
        string Name,
        int AmountInCents,
        string Currency,
        int Quantity);
}
