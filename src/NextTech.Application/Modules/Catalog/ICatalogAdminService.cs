using NextTech.Application.DTOs.Catalog;

namespace NextTech.Application.Modules.Catalog;

public interface ICatalogAdminService
{
    Task<IReadOnlyList<CategoriaAdminDto>> ListarCategoriasAsync(CancellationToken cancellationToken);

    Task<CategoriaAdminDto> CrearCategoriaAsync(
        int idUsuarioInterno,
        CrearCategoriaRequest request,
        CancellationToken cancellationToken);

    Task<CategoriaAdminDto> ActualizarCategoriaAsync(
        int idUsuarioInterno,
        int idCategoria,
        ActualizarCategoriaRequest request,
        CancellationToken cancellationToken);

    Task DesactivarCategoriaAsync(
        int idUsuarioInterno,
        int idCategoria,
        CancellationToken cancellationToken);

    Task ActivarCategoriaAsync(
        int idUsuarioInterno,
        int idCategoria,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductoAdminResumenDto>> ListarProductosAsync(CancellationToken cancellationToken);

    Task<ProductoAdminDetalleDto> ObtenerProductoAsync(
        string codigoProducto,
        CancellationToken cancellationToken);

    Task<ProductoAdminDetalleDto> CrearProductoAsync(
        int idUsuarioInterno,
        CrearProductoRequest request,
        CancellationToken cancellationToken);

    Task<ProductoAdminDetalleDto> ActualizarProductoAsync(
        int idUsuarioInterno,
        string codigoProducto,
        ActualizarProductoRequest request,
        CancellationToken cancellationToken);

    Task DesactivarProductoAsync(
        int idUsuarioInterno,
        string codigoProducto,
        CancellationToken cancellationToken);

    Task ActivarProductoAsync(
        int idUsuarioInterno,
        string codigoProducto,
        CancellationToken cancellationToken);

    Task<VarianteAdminDto> CrearVarianteAsync(
        int idUsuarioInterno,
        string codigoProducto,
        CrearVarianteRequest request,
        CancellationToken cancellationToken);

    Task<VarianteAdminDto> ActualizarVarianteAsync(
        int idUsuarioInterno,
        int idVariante,
        ActualizarVarianteRequest request,
        CancellationToken cancellationToken);

    Task DesactivarVarianteAsync(
        int idUsuarioInterno,
        int idVariante,
        CancellationToken cancellationToken);

    Task ActivarVarianteAsync(
        int idUsuarioInterno,
        int idVariante,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<AreaEntregaAdminDto>> ListarAreasAsync(CancellationToken cancellationToken);

    Task<AreaEntregaAdminDto> CrearAreaAsync(
        int idUsuarioInterno,
        CrearAreaEntregaRequest request,
        CancellationToken cancellationToken);

    Task<AreaEntregaAdminDto> ActualizarAreaAsync(
        int idUsuarioInterno,
        int idAreaEntrega,
        ActualizarAreaEntregaRequest request,
        CancellationToken cancellationToken);

    Task DesactivarAreaAsync(
        int idUsuarioInterno,
        int idAreaEntrega,
        CancellationToken cancellationToken);

    Task ActivarAreaAsync(
        int idUsuarioInterno,
        int idAreaEntrega,
        CancellationToken cancellationToken);
}
