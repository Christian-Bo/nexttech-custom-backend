using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NextTech.Application.Authentication;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.DTOs.Orders;
using NextTech.Application.Modules.Nfc;
using NextTech.Application.Modules.Orders;

namespace NextTech.Api.Controllers;

[ApiController]
[Authorize(Policy = "InternalOnly")]
[Authorize(Roles = $"{InternalRoles.Admin},{InternalRoles.Supervisor}")]
[Route("api/supervisor/orders")]
public sealed class SupervisorOrdersController(
    IOrderService orders,
    ICurrentActor actor,
    IConfiguration configuration) : ControllerBase
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

    [HttpGet("print-queue")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImprimirCola(CancellationToken cancellationToken)
    {
        _ = actor.RequireIdUsuarioInterno();
        var archivo = await orders.ObtenerHojaImpresionLoteAsync(null, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(archivo.Datos, archivo.TipoMime, archivo.NombreOriginal);
    }

    [HttpPost("print")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ImprimirLote(
        [FromBody] ImprimirLoteRequest? request,
        CancellationToken cancellationToken)
    {
        _ = actor.RequireIdUsuarioInterno();
        var archivo = await orders.ObtenerHojaImpresionLoteAsync(request?.Codigos, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(archivo.Datos, archivo.TipoMime, archivo.NombreOriginal);
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

    [HttpGet("{codigo}")]
    public async Task<IActionResult> DetalleProduccion(string codigo, CancellationToken cancellationToken)
    {
        _ = actor.RequireIdUsuarioInterno();
        var produccion = await orders.ObtenerParaProduccionAsync(codigo, cancellationToken);

        // El contenedor puede recibir HTTP desde el proxy que publica HTTPS.
        // Configuramos un origen externo fijo para el chip, independiente de Request.Scheme.
        // Si no se configura, conservamos el comportamiento existente (desarrollo local).
        var publicBaseUrl = configuration["Nfc:PublicBaseUrl"];
        if (!string.IsNullOrWhiteSpace(publicBaseUrl))
        {
            if (!Uri.TryCreate(publicBaseUrl.Trim(), UriKind.Absolute, out var publicOrigin) ||
                !string.Equals(publicOrigin.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(publicOrigin.UserInfo) ||
                publicOrigin.AbsolutePath != "/" ||
                !string.IsNullOrEmpty(publicOrigin.Query) ||
                !string.IsNullOrEmpty(publicOrigin.Fragment))
            {
                throw new InvalidOperationException(
                    "Nfc:PublicBaseUrl debe ser un origen HTTPS sin ruta, parámetros ni fragmento.");
            }

            return Ok(NfcWriteUrlMapper.Apply(produccion, publicOrigin.Scheme, publicOrigin.Authority));
        }

        return Ok(NfcWriteUrlMapper.Apply(produccion, Request.Scheme, Request.Host.Value));
    }

    [HttpGet("{codigo}/print")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> HojaImpresion(string codigo, CancellationToken cancellationToken)
    {
        _ = actor.RequireIdUsuarioInterno();
        var archivo = await orders.ObtenerHojaImpresionAsync(codigo, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(archivo.Datos, archivo.TipoMime, archivo.NombreOriginal);
    }

    [HttpGet("{codigo}/files/{idArchivo:int}")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> ArchivoProduccion(
        string codigo,
        int idArchivo,
        CancellationToken cancellationToken)
    {
        _ = actor.RequireIdUsuarioInterno();
        var archivo = await orders.ObtenerArchivoProduccionAsync(codigo, idArchivo, cancellationToken);
        Response.Headers.CacheControl = "no-store";
        return File(archivo.Datos, archivo.TipoMime, archivo.NombreOriginal);
    }
}
