using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.DTOs.Personalization;
using NextTech.Application.Modules.Personalization;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "BuyerOnly")]
[Route("api/personalizations")]
public sealed class PersonalizationsController(
    IPersonalizationService personalization,
    ICurrentActor actor) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Crear(
        [FromBody] CrearPersonalizacionRequest request,
        CancellationToken cancellationToken)
    {
        var creada = await personalization.CrearAsync(
            actor.RequireIdCompradorExterno(),
            request,
            cancellationToken);

        return Created($"/api/personalizations/{creada.IdPersonalizacion}", creada);
    }

    [HttpGet("{idPersonalizacion:int}")]
    public async Task<IActionResult> Obtener(
        int idPersonalizacion,
        CancellationToken cancellationToken)
    {
        _ = actor.RequireIdCompradorExterno();
        return Ok(await personalization.ObtenerAsync(idPersonalizacion, cancellationToken));
    }
}
