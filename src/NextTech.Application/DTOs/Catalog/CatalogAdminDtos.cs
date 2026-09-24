namespace NextTech.Application.DTOs.Catalog;

public sealed record CategoriaAdminDto(
    int IdCategoria,
    string Nombre,
    string? Descripcion,
    bool Activo);

public sealed record CrearCategoriaRequest(
    string Nombre,
    string? Descripcion);

public sealed record ActualizarCategoriaRequest(
    string Nombre,
    string? Descripcion);

public sealed record ProductoAdminResumenDto(
    int IdProducto,
    string CodigoProducto,
    string Nombre,
    string Categoria,
    decimal PrecioBase,
    bool PermitePersonalizacion,
    bool Activo,
    int VariantesActivas);

public sealed record VarianteAdminDto(
    int IdVariante,
    string CodigoVariante,
    string Nombre,
    string? Descripcion,
    decimal PrecioAdicional,
    decimal PrecioActual,
    bool Activo,
    string? FormaLienzo);

public sealed record ProductoAdminDetalleDto(
    int IdProducto,
    int IdCategoria,
    string CodigoProducto,
    string Nombre,
    string? Descripcion,
    string Categoria,
    decimal PrecioBase,
    bool PermitePersonalizacion,
    bool Activo,
    IReadOnlyList<ZonaDto> Zonas,
    IReadOnlyList<VarianteAdminDto> Variantes);

public sealed record CrearProductoRequest(
    int IdCategoria,
    string CodigoProducto,
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    bool PermitePersonalizacion);

public sealed record ActualizarProductoRequest(
    int IdCategoria,
    string Nombre,
    string? Descripcion,
    decimal PrecioBase,
    bool PermitePersonalizacion);

public sealed record CrearVarianteRequest(
    string CodigoVariante,
    string Nombre,
    string? Descripcion,
    decimal PrecioAdicional,
    string? FormaLienzo);

public sealed record ActualizarVarianteRequest(
    string Nombre,
    string? Descripcion,
    decimal PrecioAdicional,
    string? FormaLienzo);

public sealed record AreaEntregaAdminDto(
    int IdAreaEntrega,
    string Nombre,
    string? Descripcion,
    bool Activo);

public sealed record CrearAreaEntregaRequest(
    string Nombre,
    string? Descripcion);

public sealed record ActualizarAreaEntregaRequest(
    string Nombre,
    string? Descripcion);
