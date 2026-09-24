namespace NextTech.Application.Modules.Catalog;

public interface ICatalogService
{
    Task<IReadOnlyList<DTOs.Catalog.ProductoResumenDto>> ListarProductosActivosAsync(
        CancellationToken cancellationToken);

    Task<DTOs.Catalog.ProductoDetalleDto> ObtenerProductoAsync(
        string codigoProducto,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<DTOs.Catalog.AreaEntregaDto>> ListarAreasEntregaAsync(
        CancellationToken cancellationToken);
}
