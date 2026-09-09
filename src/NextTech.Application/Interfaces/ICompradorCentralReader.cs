using NextTech.Application.External;

namespace NextTech.Application.Interfaces;

/// <summary>
/// Contrato de solo lectura hacia la base Oracle central.
/// </summary>
public interface ICompradorCentralReader
{
    Task<CompradorCentralDto?> ObtenerPorIdAsync(
        long idUsuario,
        CancellationToken cancellationToken = default);

    Task<CompradorCentralDto?> ObtenerPorNicknameAsync(
        string nickname,
        CancellationToken cancellationToken = default);

    Task<CompradorCentralDto?> ObtenerPorCorreoAsync(
        string correo,
        CancellationToken cancellationToken = default);
}
