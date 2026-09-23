using NextTech.Application.DTOs.Dashboard;

namespace NextTech.Application.Modules.Dashboard;

public interface IDashboardService
{
    Task<DashboardVentasDto> ObtenerVentasAsync(
        string? rango,
        CancellationToken cancellationToken);
}
