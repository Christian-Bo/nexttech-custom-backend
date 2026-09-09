namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.PlantillaVarianteZona. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class PlantillaVarianteZona
{
    public int IdPlantilla { get; set; }
    public int IdVariante { get; set; }
    public int IdZona { get; set; }
    public string Forma { get; set; } = string.Empty;
    public int AnchoLienzo { get; set; }
    public int AltoLienzo { get; set; }
    public int? IdArchivoMascara { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaDesactivacion { get; set; }
}
