using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.DTOs.Orders;
using NextTech.Application.Modules.Orders;
using NextTech.Infrastructure.Payments;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class PaymentsController(
    IOrderService orders,
    ICurrentActor actor,
    IOptions<RecurrenteOptions> options,
    ILogger<PaymentsController> logger) : ControllerBase
{
    [Authorize(Policy = "BuyerOnly")]
    [HttpPost("payments/recurrente/confirm")]
    public async Task<IActionResult> ConfirmarTarjeta(
        [FromBody] ConfirmarPagoTarjetaRequest request,
        CancellationToken cancellationToken)
    {
        var orden = await orders.ConfirmarCheckoutTarjetaAsync(
            actor.RequireIdCompradorExterno(),
            request.CheckoutId,
            cancellationToken);

        return Created($"/api/orders/{orden.CodigoOrden}/tracking", orden);
    }

    [AllowAnonymous]
    [HttpPost("webhooks/recurrente")]
    public async Task<IActionResult> WebhookRecurrente(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
        var raw = await reader.ReadToEndAsync(cancellationToken);
        var config = options.Value;

        if (!string.IsNullOrWhiteSpace(config.WebhookSecret))
        {
            var valido = RecurrenteWebhookSignature.IsValid(
                raw,
                Request.Headers["svix-id"].ToString(),
                Request.Headers["svix-timestamp"].ToString(),
                Request.Headers["svix-signature"].ToString(),
                config.WebhookSecret,
                TimeSpan.FromMinutes(5));

            if (!valido)
            {
                return Unauthorized();
            }
        }
        else if (!HttpContext.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment())
        {
            logger.LogWarning("Webhook de Recurrente rechazado: falta WebhookSecret.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (!TryReadCheckoutId(raw, out var checkoutId))
        {
            return Ok();
        }

        try
        {
            await orders.ConfirmarCheckoutTarjetaAsync(null, checkoutId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "No se confirmó la orden desde el webhook de Recurrente.");
        }

        return Ok();
    }

    private static bool TryReadCheckoutId(string raw, out string checkoutId)
    {
        checkoutId = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (TryReadCheckoutId(doc.RootElement, out checkoutId))
            {
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }

        return false;
    }

    private static bool TryReadCheckoutId(JsonElement element, out string checkoutId)
    {
        checkoutId = string.Empty;
        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (element.TryGetProperty("checkout", out var checkout) &&
            checkout.ValueKind == JsonValueKind.Object &&
            checkout.TryGetProperty("id", out var nestedId) &&
            nestedId.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(nestedId.GetString()))
        {
            checkoutId = nestedId.GetString()!;
            return true;
        }

        if (element.TryGetProperty("checkout_id", out var checkoutIdProp) &&
            checkoutIdProp.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(checkoutIdProp.GetString()))
        {
            checkoutId = checkoutIdProp.GetString()!;
            return true;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object &&
                TryReadCheckoutId(property.Value, out checkoutId))
            {
                return true;
            }
        }

        return false;
    }
}
