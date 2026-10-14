using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NextTech.Application.Modules.Orders;

namespace NextTech.Api.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting("nfc-redirect")]
public sealed class NfcRedirectController(IOrderService orders) : ControllerBase
{
    [HttpGet("/n/{codigo}")]
    public async Task<IActionResult> IrAlDestino(string codigo, CancellationToken cancellationToken)
    {
        var destino = await orders.ObtenerUrlNfcDestinoAsync(codigo, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return Redirect(destino);
    }
}
