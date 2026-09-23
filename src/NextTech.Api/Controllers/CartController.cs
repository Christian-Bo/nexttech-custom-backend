using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.DTOs.Cart;
using NextTech.Application.Modules.Cart;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "BuyerOnly")]
[Route("api/cart")]
public sealed class CartController(
    ICartService cart,
    ICurrentActor actor) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Obtener(CancellationToken cancellationToken)
    {
        return Ok(await cart.ObtenerActivoAsync(actor.RequireIdCompradorExterno(), cancellationToken));
    }

    [HttpPost("items")]
    public async Task<IActionResult> Agregar(
        [FromBody] AgregarItemCarritoRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await cart.AgregarItemAsync(
            actor.RequireIdCompradorExterno(),
            request,
            cancellationToken));
    }

    [HttpPatch("items/{idDetalleCarrito:int}")]
    public async Task<IActionResult> CambiarCantidad(
        int idDetalleCarrito,
        [FromBody] CambiarCantidadRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await cart.CambiarCantidadAsync(
            actor.RequireIdCompradorExterno(),
            idDetalleCarrito,
            request.Cantidad,
            cancellationToken));
    }

    [HttpDelete("items/{idDetalleCarrito:int}")]
    public async Task<IActionResult> Eliminar(
        int idDetalleCarrito,
        CancellationToken cancellationToken)
    {
        return Ok(await cart.EliminarItemAsync(
            actor.RequireIdCompradorExterno(),
            idDetalleCarrito,
            cancellationToken));
    }
}
