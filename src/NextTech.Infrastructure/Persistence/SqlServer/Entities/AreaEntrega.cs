namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.AreaEntrega. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class AreaEntrega
{
    public int IdAreaEntrega { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaDesactivacion { get; set; }
}
