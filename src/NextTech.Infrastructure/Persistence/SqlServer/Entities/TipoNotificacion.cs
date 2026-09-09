namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.TipoNotificacion. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class TipoNotificacion
{
    public int IdTipoNotificacion { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
