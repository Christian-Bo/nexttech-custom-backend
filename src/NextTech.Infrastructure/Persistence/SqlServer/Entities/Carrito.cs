namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.Carrito. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class Carrito
{
    public int IdCarrito { get; set; }
    public long IdCompradorExterno { get; set; }
    public int IdEstadoCarrito { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime UltimaActividad { get; set; }
}
