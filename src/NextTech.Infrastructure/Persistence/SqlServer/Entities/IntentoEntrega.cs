namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.IntentoEntrega. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class IntentoEntrega
{
    public int IdIntentoEntrega { get; set; }
    public int IdOrden { get; set; }
    public int IdRepartidor { get; set; }
    public DateTime FechaHoraInicio { get; set; }
    public DateTime? FechaHoraFin { get; set; }
    public int? IdResultadoEntrega { get; set; }
    public string? Observacion { get; set; }
    public int? IdArchivoFotoEntrega { get; set; }
}
