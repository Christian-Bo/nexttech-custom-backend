namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.ProductoAtributo. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class ProductoAtributo
{
    public int IdProductoAtributo { get; set; }
    public int IdProducto { get; set; }
    public int IdAtributo { get; set; }
    public bool EsObligatorio { get; set; }
    public int OrdenVisual { get; set; }
}
