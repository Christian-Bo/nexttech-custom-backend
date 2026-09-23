namespace NextTech.Application.DTOs.Dashboard;

public sealed record DashboardVentasDto(
    string Rango,
    DateTime Desde,
    DateTime Hasta,
    int TotalOrdenes,
    decimal TotalVentas,
    IReadOnlyList<DashboardConteoDto> PorEstado,
    IReadOnlyList<DashboardConteoDto> PorProducto);

public sealed record DashboardConteoDto(
    string Nombre,
    int Cantidad,
    decimal Total);
