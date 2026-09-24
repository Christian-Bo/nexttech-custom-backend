using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.DTOs.Orders;
using NextTech.Application.Interfaces;
using NextTech.Application.Modules.Orders;
using NextTech.Domain.Exceptions;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "BuyerOnly")]
[Route("api")]
public sealed class OrdersController(
    IOrderService orders,
    ICurrentActor actor,
    ICompradorCentralReader compradores) : ControllerBase
{
    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutRequest request,
        CancellationToken cancellationToken)
    {
        var id = actor.RequireIdCompradorExterno();
        var central = await compradores.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException("El comprador autenticado no existe en Oracle.");

        var orden = await orders.CheckoutEfectivoAsync(
            id,
            central.Nickname,
            central.Correo,
            central.Telefono,
            central.NotificaEmail,
            central.NotificaWhatsApp,
            request,
            cancellationToken);

        return Created($"/api/orders/{orden.CodigoOrden}/tracking", orden);
    }

    [HttpGet("orders/my")]
    public async Task<IActionResult> MisCompras(CancellationToken cancellationToken)
    {
        return Ok(await orders.ObtenerMisComprasAsync(
            actor.RequireIdCompradorExterno(),
            cancellationToken));
    }

    [HttpGet("orders/{codigo}/tracking")]
    public async Task<IActionResult> Tracking(string codigo, CancellationToken cancellationToken)
    {
        return Ok(await orders.ObtenerSeguimientoAsync(
            actor.RequireIdCompradorExterno(),
            codigo,
            cancellationToken));
    }

    [HttpGet("orders/{codigo}/receipt")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Constancia(string codigo, CancellationToken cancellationToken)
    {
        var archivo = await orders.ObtenerConstanciaAsync(
            actor.RequireIdCompradorExterno(),
            codigo,
            cancellationToken);

        Response.Headers.CacheControl = "no-store";
        return File(archivo.Datos, archivo.TipoMime, archivo.NombreOriginal);
    }

    [HttpGet("orders/{codigo}/receipt-qr")]
    [Produces("image/png")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> QrConstancia(string codigo, CancellationToken cancellationToken)
    {
        var archivo = await orders.ObtenerQrConstanciaAsync(
            actor.RequireIdCompradorExterno(),
            codigo,
            cancellationToken);

        Response.Headers.CacheControl = "no-store";
        return File(archivo.Datos, archivo.TipoMime, archivo.NombreOriginal);
    }
}
