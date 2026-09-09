namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.BitacoraAuditoria. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class BitacoraAuditoria
{
    public int IdBitacora { get; set; }
    public int IdTipoActor { get; set; }
    public int? IdUsuarioInterno { get; set; }
    public long? IdCompradorExterno { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public int? IdEntidad { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public string? DireccionIp { get; set; }
    public string? Detalle { get; set; }
    public DateTime FechaHora { get; set; }
}
