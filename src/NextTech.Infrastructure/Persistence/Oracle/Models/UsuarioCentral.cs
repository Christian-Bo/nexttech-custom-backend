namespace NextTech.Infrastructure.Persistence.Oracle.Models;

/// <summary>
/// Proyección de solo lectura de TIENDA_APP.USUARIO.
/// Se mapean únicamente los campos que NextTech necesita.
/// </summary>
public sealed class UsuarioCentral
{
    public long IdUsuario { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public int NotificaEmail { get; set; }
    public int NotificaWhatsApp { get; set; }
}
