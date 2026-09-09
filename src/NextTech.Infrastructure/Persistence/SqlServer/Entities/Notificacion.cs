namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.Notificacion. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class Notificacion
{
    public int IdNotificacion { get; set; }
    public int IdOrden { get; set; }
    public int IdTipoNotificacion { get; set; }
    public int IdCanalNotificacion { get; set; }
    public int IdEstadoNotificacion { get; set; }
    public string Destino { get; set; } = string.Empty;
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public int Intentos { get; set; }
    public string? MensajeError { get; set; }
}
