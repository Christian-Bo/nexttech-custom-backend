namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.ValorAtributo. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class ValorAtributo
{
    public int IdValorAtributo { get; set; }
    public int IdAtributo { get; set; }
    public string Valor { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaDesactivacion { get; set; }
}
