using Microsoft.EntityFrameworkCore;
using NextTech.Application.DTOs.Dashboard;
using NextTech.Application.Modules.Dashboard;
using NextTech.Domain.Exceptions;
using NextTech.Infrastructure.Persistence.SqlServer;

namespace NextTech.Infrastructure.Store;

public sealed class DashboardService(NextTechDbContext db) : IDashboardService
{
    public async Task<DashboardVentasDto> ObtenerVentasAsync(
        string? rango,
        CancellationToken cancellationToken)
    {
        var clave = (rango ?? "week").Trim().ToLowerInvariant();
        var hasta = DateTime.Now;
        var desde = clave switch
        {
            "day" or "dia" => hasta.Date,
            "week" or "semana" => hasta.Date.AddDays(-6),
            "total" => DateTime.MinValue,
            _ => throw new BusinessRuleException("El rango debe ser day, week o total.")
        };

        var ordenes = await db.Orden.AsNoTracking()
            .Where(o => clave == "total" || o.FechaCreacion >= desde)
            .ToListAsync(cancellationToken);

        var estados = await db.EstadoOrden.AsNoTracking()
            .ToDictionaryAsync(e => e.IdEstadoOrden, e => e.Nombre, cancellationToken);

        var porEstado = ordenes
            .GroupBy(o => estados.GetValueOrDefault(o.IdEstadoOrdenActual, "Desconocido"))
            .Select(g => new DashboardConteoDto(g.Key, g.Count(), g.Sum(x => x.Total)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var ids = ordenes.Select(o => o.IdOrden).ToList();
        var detalles = ids.Count == 0
            ? []
            : await db.DetalleOrden.AsNoTracking()
                .Where(d => ids.Contains(d.IdOrden))
                .ToListAsync(cancellationToken);

        var porProducto = detalles
            .GroupBy(d => d.NombreProductoAplicado)
            .Select(g => new DashboardConteoDto(g.Key, g.Sum(x => x.Cantidad), g.Sum(x => x.Subtotal)))
            .OrderByDescending(x => x.Total)
            .ToList();

        var desdeRespuesta = clave == "total"
            ? (ordenes.Count == 0 ? hasta.Date : ordenes.Min(o => o.FechaCreacion))
            : desde;

        return new DashboardVentasDto(
            clave == "dia" ? "day" : clave == "semana" ? "week" : clave,
            desdeRespuesta,
            hasta,
            ordenes.Count,
            ordenes.Sum(o => o.Total),
            porEstado,
            porProducto);
    }
}
