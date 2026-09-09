namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.Pago. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class Pago
{
    public int IdPago { get; set; }
    public int IdOrden { get; set; }
    public int IdMetodoPago { get; set; }
    public int IdEstadoPago { get; set; }
    public decimal Monto { get; set; }
    public string? ReferenciaTransaccion { get; set; }
    public string? MarcaTarjeta { get; set; }
    public string? Ultimos4 { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaPago { get; set; }
}
