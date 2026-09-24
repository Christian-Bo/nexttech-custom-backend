using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.DTOs.Orders;
using NextTech.Application.Modules.Orders;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "DeliveryDriverOnly")]
[Route("api/delivery")]
public sealed class DeliveryController(
    IOrderService orders,
    ICurrentActor actor) : ControllerBase
{
    [HttpGet("available")]
    public async Task<IActionResult> Disponibles(CancellationToken cancellationToken)
    {
        return Ok(await orders.ListarDisponiblesEntregaAsync(cancellationToken));
    }

    [HttpGet("orders/{codigo}")]
    public async Task<IActionResult> Buscar(string codigo, CancellationToken cancellationToken)
    {
        _ = actor.RequireIdUsuarioInterno();
        return Ok(await orders.BuscarParaEntregaAsync(codigo, cancellationToken));
    }

    [HttpPost("{codigo}/take")]
    public async Task<IActionResult> Tomar(string codigo, CancellationToken cancellationToken)
    {
        await orders.TomarOrdenAsync(actor.RequireIdUsuarioInterno(), codigo, cancellationToken);
        return NoContent();
    }

    [HttpPost("{codigo}/complete")]
    public async Task<IActionResult> Completar(
        string codigo,
        [FromBody] ConfirmarEntregaRequest request,
        CancellationToken cancellationToken)
    {
        await orders.ConfirmarEntregaAsync(
            actor.RequireIdUsuarioInterno(),
            codigo,
            request.FotoBase64,
            request.NombreArchivo,
            request.TipoMime,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{codigo}/not-found")]
    public async Task<IActionResult> NoEncontrado(
        string codigo,
        [FromBody] NoEncontradoRequest request,
        CancellationToken cancellationToken)
    {
        await orders.RegistrarNoEncontradoAsync(
            actor.RequireIdUsuarioInterno(),
            codigo,
            request.Observacion,
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{codigo}/payment-failed")]
    public async Task<IActionResult> PagoNoRealizado(
        string codigo,
        [FromBody] PagoNoRealizadoRequest request,
        CancellationToken cancellationToken)
    {
        await orders.RegistrarPagoNoRealizadoAsync(
            actor.RequireIdUsuarioInterno(),
            codigo,
            request.Observacion,
            cancellationToken);

        return NoContent();
    }
}
