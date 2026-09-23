namespace NextTech.Application.DTOs.Orders;

public sealed record CheckoutRequest(
    int IdAreaEntrega,
    string ReferenciaEntrega,
    string MetodoPago);

public sealed record OrdenResumenDto(
    string CodigoOrden,
    DateTime FechaCreacion,
    decimal Total,
    string Estado);

public sealed record OrdenDetalleDto(
    string CodigoOrden,
    DateTime FechaCreacion,
    decimal Total,
    string Estado,
    string NicknameComprador,
    string AreaEntrega,
    string ReferenciaEntrega,
    string MetodoPago,
    string EstadoPago,
    IReadOnlyList<DetalleOrdenDto> Items,
    IReadOnlyList<TrackingEventoDto> Tracking,
    IReadOnlyList<TrackingPasoDto> Pasos);

public sealed record DetalleOrdenDto(
    string NombreProducto,
    string NombreVariante,
    int Cantidad,
    decimal PrecioUnitario,
    decimal Subtotal,
    int? IdPersonalizacion);

public sealed record TrackingEventoDto(
    string Estado,
    DateTime FechaHora,
    string? Observacion);

public sealed record TrackingPasoDto(
    int Orden,
    string Codigo,
    string Nombre,
    bool Completado,
    bool Actual);

public sealed record OrdenColaDto(
    string CodigoOrden,
    DateTime FechaCreacion,
    decimal Total,
    string Estado,
    string AreaEntrega);

public sealed record ConfirmarEntregaRequest(
    string FotoBase64,
    string NombreArchivo,
    string TipoMime);

public sealed record NoEncontradoRequest(string Observacion);

public sealed record OrdenEntregaDto(
    string CodigoOrden,
    DateTime FechaCreacion,
    decimal Total,
    string Estado,
    string AreaEntrega,
    string ReferenciaEntrega,
    string NicknameComprador,
    string EstadoPago);
