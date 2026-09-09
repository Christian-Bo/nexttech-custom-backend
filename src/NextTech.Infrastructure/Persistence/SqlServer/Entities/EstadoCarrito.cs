namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.EstadoCarrito. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class EstadoCarrito
{
    public int IdEstadoCarrito { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
}
