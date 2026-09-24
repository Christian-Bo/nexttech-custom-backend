namespace NextTech.Application.DTOs.Catalog;

public sealed record ProductoResumenDto(
    string CodigoProducto,
    string Nombre,
    string? Descripcion,
    string Categoria,
    decimal PrecioDesde,
    bool PermitePersonalizacion);

public sealed record ProductoDetalleDto(
    string CodigoProducto,
    string Nombre,
    string? Descripcion,
    string Categoria,
    decimal PrecioBase,
    bool PermitePersonalizacion,
    MedidasFisicasDto Medidas,
    IReadOnlyList<VarianteDto> Variantes,
    IReadOnlyList<ZonaDto> Zonas);

public sealed record VarianteDto(
    int IdVariante,
    string CodigoVariante,
    string Nombre,
    decimal PrecioAdicional,
    decimal PrecioActual,
    IReadOnlyList<AtributoValorDto> Atributos,
    IReadOnlyList<PlantillaZonaDto> Plantillas);

public sealed record AtributoValorDto(
    string Atributo,
    string Valor);

public sealed record ZonaDto(
    int IdZona,
    string Nombre,
    bool EsObligatoria,
    int OrdenVisual);

public sealed record PlantillaZonaDto(
    int IdZona,
    string Forma,
    int AnchoLienzo,
    int AltoLienzo);

public sealed record MedidasFisicasDto(
    decimal DiametroLlaveroPulgadas,
    decimal DiametroNfcPulgadas,
    decimal DiametroLlaveroMm,
    decimal DiametroNfcMm,
    int LienzoPx,
    int NfcLienzoPx);

public sealed record AreaEntregaDto(
    int IdAreaEntrega,
    string Nombre,
    string? Descripcion);
