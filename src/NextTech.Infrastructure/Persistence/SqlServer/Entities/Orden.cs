namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.Orden. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class Orden
{
    public int IdOrden { get; set; }
    public string CodigoOrden { get; set; } = string.Empty;
    public long IdCompradorExterno { get; set; }
    public string NicknameCompradorAplicado { get; set; } = string.Empty;
    public string CorreoCompradorAplicado { get; set; } = string.Empty;
    public string? TelefonoCompradorAplicado { get; set; }
    public int? IdCarritoOrigen { get; set; }
    public int IdAreaEntrega { get; set; }
    public string NombreAreaAplicado { get; set; } = string.Empty;
    public string ReferenciaEntrega { get; set; } = string.Empty;
    public int IdEstadoOrdenActual { get; set; }
    public int? IdRepartidorAsignado { get; set; }
    public decimal Total { get; set; }
    public int? IdArchivoConstancia { get; set; }
    public bool QrUtilizado { get; set; }
    public DateTime? FechaUsoQr { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
