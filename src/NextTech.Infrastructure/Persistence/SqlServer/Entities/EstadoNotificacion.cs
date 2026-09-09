namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.EstadoNotificacion. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class EstadoNotificacion
{
    public int IdEstadoNotificacion { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
