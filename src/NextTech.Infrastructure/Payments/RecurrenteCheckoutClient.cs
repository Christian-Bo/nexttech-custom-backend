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

        var customer = await EnsureCustomerAsync(
            request.CustomerEmail,
            request.CustomerName,
            request.CustomerPhone,
            cancellationToken);

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
                    request.Metadata,
                    customer?.Id,
                    customer?.UserId),
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

    private async Task<RecurrenteCustomerRef?> EnsureCustomerAsync(
        string email,
        string? name,
        string? phone,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var nombre = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        var telefono = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        try
        {
            using var create = new HttpRequestMessage(HttpMethod.Post, "api/customers")
            {
                Content = JsonContent.Create(
                    new RecurrenteCustomerCreateBody(email.Trim(), nombre, nombre, telefono),
                    options: Json)
            };
            AddKeys(create);

            using var created = await http.SendAsync(create, cancellationToken);
            var createdJson = await created.Content.ReadAsStringAsync(cancellationToken);
            if (!created.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Recurrente respondió {Status} al crear el cliente del checkout.",
                    (int)created.StatusCode);
                return null;
            }

            var customer = RecurrenteCheckoutParser.TryParseCustomer(createdJson);
            if (customer is null)
            {
                return null;
            }

            if (nombre is null && telefono is null)
            {
                return customer;
            }

            using var update = new HttpRequestMessage(
                HttpMethod.Put,
                $"api/customers/{Uri.EscapeDataString(customer.Id)}")
            {
                Content = JsonContent.Create(
                    new RecurrenteCustomerUpdateBody(nombre, telefono),
                    options: Json)
            };
            AddKeys(update);

            using var updated = await http.SendAsync(update, cancellationToken);
            if (!updated.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Recurrente respondió {Status} al actualizar el cliente del checkout.",
                    (int)updated.StatusCode);
            }

            return customer;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se pudo precargar el cliente en Recurrente.");
            return null;
        }
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
        IReadOnlyDictionary<string, string> Metadata,
        string? CustomerId,
        string? UserId);

    private sealed record RecurrenteItem(
        string Name,
        int AmountInCents,
        string Currency,
        int Quantity);

    private sealed record RecurrenteCustomerCreateBody(
        string Email,
        string? FullName,
        string? Name,
        string? Phone);

    private sealed record RecurrenteCustomerUpdateBody(
        string? Name,
        string? Phone);
}
