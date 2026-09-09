namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.ProductoImagen. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class ProductoImagen
{
    public int IdProductoImagen { get; set; }
    public int IdProducto { get; set; }
    public int IdArchivo { get; set; }
    public bool EsPrincipal { get; set; }
    public int OrdenVisual { get; set; }
}
