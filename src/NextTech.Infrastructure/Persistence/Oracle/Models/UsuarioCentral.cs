namespace NextTech.Infrastructure.Persistence.Oracle.Models;

/// <summary>Proyección del comprador central. Oracle sigue siendo la fuente de verdad.</summary>
public sealed class UsuarioCentral
{
    public long IdUsuario { get; set; }
    public long? IdFotoOriginal { get; set; }
    public long? IdFotoModificada { get; set; }
    public string Correo { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string TokenQrHash { get; set; } = string.Empty;
    public string NotificaEmail { get; set; } = "S";
    public string NotificaWhatsApp { get; set; } = "N";
    public string Activo { get; set; } = "S";
    public string Bloqueado { get; set; } = "N";
    public int IntentosFallidos { get; set; }
}
