using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Authentication;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Modules.Orders;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalOnly")]
[Authorize(Roles = $"{InternalRoles.Admin},{InternalRoles.Supervisor}")]
[Route("api/supervisor/orders")]
public sealed class SupervisorOrdersController(
    IOrderService orders,
    ICurrentActor actor) : ControllerBase
{
    [HttpGet("in-production")]
    public async Task<IActionResult> EnElaboracion(CancellationToken cancellationToken)
    {
        return Ok(await orders.ListarEnElaboracionAsync(cancellationToken));
    }

    [HttpGet("ready")]
    public async Task<IActionResult> Listos(CancellationToken cancellationToken)
    {
        return Ok(await orders.ListarListosParaEntregaAsync(cancellationToken));
    }

    [HttpPost("{codigo}/ready")]
    public async Task<IActionResult> MarcarListo(string codigo, CancellationToken cancellationToken)
    {
        await orders.MarcarListoParaEntregaAsync(
            actor.RequireIdUsuarioInterno(),
            codigo,
            cancellationToken);

        return NoContent();
    }
}
