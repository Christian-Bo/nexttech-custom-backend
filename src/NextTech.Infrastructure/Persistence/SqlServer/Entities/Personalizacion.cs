namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.Personalizacion. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class Personalizacion
{
    public int IdPersonalizacion { get; set; }
    public int IdVariante { get; set; }
    public bool Bloqueada { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
