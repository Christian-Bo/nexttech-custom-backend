namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.HistorialEstadoOrden. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class HistorialEstadoOrden
{
    public int IdHistorialEstado { get; set; }
    public int IdOrden { get; set; }
    public int IdEstadoOrden { get; set; }
    public int IdTipoActor { get; set; }
    public int? IdUsuarioInterno { get; set; }
    public long? IdCompradorExterno { get; set; }
    public DateTime FechaHora { get; set; }
    public string? Observacion { get; set; }
}
