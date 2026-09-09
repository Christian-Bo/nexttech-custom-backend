namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.ZonaPersonalizacion. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class ZonaPersonalizacion
{
    public int IdZona { get; set; }
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool EsObligatoria { get; set; }
    public int OrdenVisual { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaDesactivacion { get; set; }
}
