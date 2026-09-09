namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.VarianteAtributo. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class VarianteAtributo
{
    public int IdVarianteAtributo { get; set; }
    public int IdVariante { get; set; }
    public int IdAtributo { get; set; }
    public int IdValorAtributo { get; set; }
}
