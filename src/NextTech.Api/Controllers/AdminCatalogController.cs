using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.DTOs.Catalog;
using NextTech.Application.Modules.Catalog;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/catalog")]
public sealed class AdminCatalogController(
    ICatalogAdminService catalog,
    ICurrentActor actor) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<IActionResult> Categorias(CancellationToken cancellationToken)
        => Ok(await catalog.ListarCategoriasAsync(cancellationToken));

    [HttpPost("categories")]
    public async Task<IActionResult> CrearCategoria(
        [FromBody] CrearCategoriaRequest request,
        CancellationToken cancellationToken)
    {
        var creada = await catalog.CrearCategoriaAsync(actor.RequireIdUsuarioInterno(), request, cancellationToken);
        return Created($"/api/admin/catalog/categories/{creada.IdCategoria}", creada);
    }

    [HttpPut("categories/{idCategoria:int}")]
    public async Task<IActionResult> ActualizarCategoria(
        int idCategoria,
        [FromBody] ActualizarCategoriaRequest request,
        CancellationToken cancellationToken)
        => Ok(await catalog.ActualizarCategoriaAsync(
            actor.RequireIdUsuarioInterno(),
            idCategoria,
            request,
            cancellationToken));

    [HttpPost("categories/{idCategoria:int}/deactivate")]
    public async Task<IActionResult> DesactivarCategoria(int idCategoria, CancellationToken cancellationToken)
    {
        await catalog.DesactivarCategoriaAsync(actor.RequireIdUsuarioInterno(), idCategoria, cancellationToken);
        return NoContent();
    }

    [HttpPost("categories/{idCategoria:int}/activate")]
    public async Task<IActionResult> ActivarCategoria(int idCategoria, CancellationToken cancellationToken)
    {
        await catalog.ActivarCategoriaAsync(actor.RequireIdUsuarioInterno(), idCategoria, cancellationToken);
        return NoContent();
    }

    [HttpGet("products")]
    public async Task<IActionResult> Productos(CancellationToken cancellationToken)
        => Ok(await catalog.ListarProductosAsync(cancellationToken));

    [HttpGet("products/{codigo}")]
    public async Task<IActionResult> Producto(string codigo, CancellationToken cancellationToken)
        => Ok(await catalog.ObtenerProductoAsync(codigo, cancellationToken));

    [HttpPost("products")]
    public async Task<IActionResult> CrearProducto(
        [FromBody] CrearProductoRequest request,
        CancellationToken cancellationToken)
    {
        var creado = await catalog.CrearProductoAsync(actor.RequireIdUsuarioInterno(), request, cancellationToken);
        return Created($"/api/admin/catalog/products/{creado.CodigoProducto}", creado);
    }

    [HttpPut("products/{codigo}")]
    public async Task<IActionResult> ActualizarProducto(
        string codigo,
        [FromBody] ActualizarProductoRequest request,
        CancellationToken cancellationToken)
        => Ok(await catalog.ActualizarProductoAsync(
            actor.RequireIdUsuarioInterno(),
            codigo,
            request,
            cancellationToken));

    [HttpPost("products/{codigo}/deactivate")]
    public async Task<IActionResult> DesactivarProducto(string codigo, CancellationToken cancellationToken)
    {
        await catalog.DesactivarProductoAsync(actor.RequireIdUsuarioInterno(), codigo, cancellationToken);
        return NoContent();
    }

    [HttpPost("products/{codigo}/activate")]
    public async Task<IActionResult> ActivarProducto(string codigo, CancellationToken cancellationToken)
    {
        await catalog.ActivarProductoAsync(actor.RequireIdUsuarioInterno(), codigo, cancellationToken);
        return NoContent();
    }

    [HttpPost("products/{codigo}/variants")]
    public async Task<IActionResult> CrearVariante(
        string codigo,
        [FromBody] CrearVarianteRequest request,
        CancellationToken cancellationToken)
    {
        var creada = await catalog.CrearVarianteAsync(
            actor.RequireIdUsuarioInterno(),
            codigo,
            request,
            cancellationToken);
        return Created($"/api/admin/catalog/variants/{creada.IdVariante}", creada);
    }

    [HttpPut("variants/{idVariante:int}")]
    public async Task<IActionResult> ActualizarVariante(
        int idVariante,
        [FromBody] ActualizarVarianteRequest request,
        CancellationToken cancellationToken)
        => Ok(await catalog.ActualizarVarianteAsync(
            actor.RequireIdUsuarioInterno(),
            idVariante,
            request,
            cancellationToken));

    [HttpPost("variants/{idVariante:int}/deactivate")]
    public async Task<IActionResult> DesactivarVariante(int idVariante, CancellationToken cancellationToken)
    {
        await catalog.DesactivarVarianteAsync(actor.RequireIdUsuarioInterno(), idVariante, cancellationToken);
        return NoContent();
    }

    [HttpPost("variants/{idVariante:int}/activate")]
    public async Task<IActionResult> ActivarVariante(int idVariante, CancellationToken cancellationToken)
    {
        await catalog.ActivarVarianteAsync(actor.RequireIdUsuarioInterno(), idVariante, cancellationToken);
        return NoContent();
    }

    [HttpGet("delivery-areas")]
    public async Task<IActionResult> Areas(CancellationToken cancellationToken)
        => Ok(await catalog.ListarAreasAsync(cancellationToken));

    [HttpPost("delivery-areas")]
    public async Task<IActionResult> CrearArea(
        [FromBody] CrearAreaEntregaRequest request,
        CancellationToken cancellationToken)
    {
        var creada = await catalog.CrearAreaAsync(actor.RequireIdUsuarioInterno(), request, cancellationToken);
        return Created($"/api/admin/catalog/delivery-areas/{creada.IdAreaEntrega}", creada);
    }

    [HttpPut("delivery-areas/{idArea:int}")]
    public async Task<IActionResult> ActualizarArea(
        int idArea,
        [FromBody] ActualizarAreaEntregaRequest request,
        CancellationToken cancellationToken)
        => Ok(await catalog.ActualizarAreaAsync(
            actor.RequireIdUsuarioInterno(),
            idArea,
            request,
            cancellationToken));

    [HttpPost("delivery-areas/{idArea:int}/deactivate")]
    public async Task<IActionResult> DesactivarArea(int idArea, CancellationToken cancellationToken)
    {
        await catalog.DesactivarAreaAsync(actor.RequireIdUsuarioInterno(), idArea, cancellationToken);
        return NoContent();
    }

    [HttpPost("delivery-areas/{idArea:int}/activate")]
    public async Task<IActionResult> ActivarArea(int idArea, CancellationToken cancellationToken)
    {
        await catalog.ActivarAreaAsync(actor.RequireIdUsuarioInterno(), idArea, cancellationToken);
        return NoContent();
    }
}
