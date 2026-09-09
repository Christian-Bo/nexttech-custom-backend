namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.DetalleCarrito. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class DetalleCarrito
{
    public int IdDetalleCarrito { get; set; }
    public int IdCarrito { get; set; }
    public int IdVariante { get; set; }
    public int Cantidad { get; set; }
    public int? IdPersonalizacion { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
