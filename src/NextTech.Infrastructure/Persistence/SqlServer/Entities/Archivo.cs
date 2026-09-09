namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

/// <summary>
/// Mapeo de la tabla dbo.Archivo. La base SQL Server es la fuente de verdad.
/// </summary>
public sealed class Archivo
{
    public int IdArchivo { get; set; }
    public string NombreOriginal { get; set; } = string.Empty;
    public string TipoMime { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public byte[] Datos { get; set; } = Array.Empty<byte>();
    public long TamanoBytes { get; set; }
    public DateTime FechaCreacion { get; set; }
}
