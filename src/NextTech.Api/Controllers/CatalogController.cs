using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Modules.Catalog;

namespace NextTech.Api.Controllers;

[ApiController]
[Route("api/catalog")]
public sealed class CatalogController(ICatalogService catalog) : ControllerBase
{
    [HttpGet("products")]
    public async Task<IActionResult> Listar(CancellationToken cancellationToken)
    {
        return Ok(await catalog.ListarProductosActivosAsync(cancellationToken));
    }

    [HttpGet("products/{codigo}")]
    public async Task<IActionResult> Detalle(string codigo, CancellationToken cancellationToken)
    {
        return Ok(await catalog.ObtenerProductoAsync(codigo, cancellationToken));
    }

    [HttpGet("delivery-areas")]
    public async Task<IActionResult> Areas(CancellationToken cancellationToken)
    {
        return Ok(await catalog.ListarAreasEntregaAsync(cancellationToken));
    }
}
