namespace NextTech.Infrastructure.Persistence.SqlServer.Entities;

public sealed class BuyerBiometricCredentialEntity
{
    public long IdBiometriaComprador { get; set; }
    public long IdCompradorExterno { get; set; }
    public string BiometricTemplate { get; set; } = string.Empty;
    public string TemplateVersion { get; set; } = string.Empty;
    public string TemplateKeyId { get; set; } = string.Empty;
    public string TemplateModel { get; set; } = string.Empty;
    public int TemplateDimensions { get; set; }
    public string? EmbeddingSha256 { get; set; }
    public byte[] RetratoDatos { get; set; } = Array.Empty<byte>();
    public string RetratoMime { get; set; } = string.Empty;
    public int RetratoAncho { get; set; }
    public int RetratoAlto { get; set; }
    public string? RetratoFondo { get; set; }
    public bool Activo { get; set; }
    public DateTime FechaEnrolamiento { get; set; }
    public DateTime? FechaActualizacion { get; set; }
}
