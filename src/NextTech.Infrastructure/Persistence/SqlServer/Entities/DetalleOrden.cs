namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.DetalleOrden. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class DetalleOrden
{
    public int IdDetalleOrden { get; set; }
    public int IdOrden { get; set; }
    public int IdVariante { get; set; }
    public int? IdPersonalizacion { get; set; }
    public int Cantidad { get; set; }
    public string NombreProductoAplicado { get; set; } = string.Empty;
    public string NombreVarianteAplicada { get; set; } = string.Empty;
    public string AtributosAplicadosJson { get; set; } = string.Empty;
    public decimal PrecioBaseAplicado { get; set; }
    public decimal PrecioVarianteAplicado { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}
