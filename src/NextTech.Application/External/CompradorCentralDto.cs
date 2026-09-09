namespace NextTech.Application.External;

/// <summary>
/// Datos mínimos del comprador que NextTech necesita leer desde Oracle.
/// No representa ni duplica toda la cuenta central.
/// </summary>
public sealed record CompradorCentralDto(
    long IdUsuario,
    string Correo,
    string? Telefono,
    DateTime? FechaNacimiento,
    string Nickname,
    bool NotificaEmail,
    bool NotificaWhatsApp);
