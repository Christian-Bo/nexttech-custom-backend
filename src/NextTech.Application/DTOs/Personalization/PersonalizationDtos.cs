namespace NextTech.Application.DTOs.Personalization;

public sealed record CrearPersonalizacionRequest(
    int IdVariante,
    IReadOnlyList<ZonaPersonalizacionRequest> Zonas);

public sealed record ZonaPersonalizacionRequest(
    int IdZona,
    string ConfiguracionJson,
    string ImagenBase64,
    string NombreArchivo,
    string TipoMime);

public sealed record PersonalizacionDto(
    int IdPersonalizacion,
    int IdVariante,
    bool Bloqueada,
    IReadOnlyList<ZonaPersonalizacionDto> Zonas);

public sealed record ZonaPersonalizacionDto(
    int IdZona,
    int IdArchivoImagenFinal,
    string ConfiguracionJson);
