using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Authentication;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Modules.Orders;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalOnly")]
[Authorize(Roles = $"{InternalRoles.Admin},{InternalRoles.Supervisor}")]
[Route("api/supervisor/files")]
public sealed class SupervisorFilesController(
    IOrderService orders,
    ICurrentActor actor) : ControllerBase
{
    [HttpGet("{idArchivo:int}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> Descargar(int idArchivo, CancellationToken cancellationToken)
    {
        _ = actor.RequireIdUsuarioInterno();
        var archivo = await orders.ObtenerArchivoProduccionPorIdAsync(idArchivo, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(archivo.Datos, archivo.TipoMime, archivo.NombreOriginal);
    }
}
