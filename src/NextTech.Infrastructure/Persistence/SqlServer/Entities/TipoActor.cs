namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.TipoActor. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class TipoActor
{
    public int IdTipoActor { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
