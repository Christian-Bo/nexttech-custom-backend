using Microsoft.EntityFrameworkCore;
using NextTech.Application.External;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.Persistence.Oracle.Repositories;

public sealed class CompradorCentralReader(OracleDbContext dbContext) : ICompradorCentralReader
{
    public async Task<CompradorCentralDto?> ObtenerPorIdAsync(
        long idUsuario,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdUsuario == idUsuario, cancellationToken);

        return user is null ? null : ToDto(user);
    }

    public async Task<CompradorCentralDto?> ObtenerPorNicknameAsync(
        string nickname,
        CancellationToken cancellationToken = default)
    {
        var normalized = nickname.Trim().ToLowerInvariant();
        var user = await dbContext.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Nickname.ToLower() == normalized, cancellationToken);

        return user is null ? null : ToDto(user);
    }

    public async Task<CompradorCentralDto?> ObtenerPorCorreoAsync(
        string correo,
        CancellationToken cancellationToken = default)
    {
        var normalized = correo.Trim().ToLowerInvariant();
        var user = await dbContext.Usuarios
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Correo.ToLower() == normalized, cancellationToken);

        return user is null ? null : ToDto(user);
    }

    private static CompradorCentralDto ToDto(Models.UsuarioCentral x)
        => new(
            x.IdUsuario,
            x.Correo,
            x.Telefono,
            x.FechaNacimiento,
            x.Nickname,
            IsYes(x.NotificaEmail),
            IsYes(x.NotificaWhatsApp));

    private static bool IsYes(string? value)
        => string.Equals(value?.Trim(), "S", StringComparison.OrdinalIgnoreCase);
}
