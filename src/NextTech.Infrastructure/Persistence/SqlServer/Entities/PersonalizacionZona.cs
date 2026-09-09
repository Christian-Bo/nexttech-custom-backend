namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.PersonalizacionZona. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class PersonalizacionZona
{
    public int IdPersonalizacionZona { get; set; }
    public int IdPersonalizacion { get; set; }
    public int IdZona { get; set; }
    public int IdArchivoImagenFinal { get; set; }
    public string ConfiguracionJson { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
