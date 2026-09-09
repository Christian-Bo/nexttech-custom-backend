namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.VarianteProducto. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class VarianteProducto
{
    public int IdVariante { get; set; }
    public int IdProducto { get; set; }
    public string CodigoVariante { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal PrecioAdicional { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaDesactivacion { get; set; }
}
