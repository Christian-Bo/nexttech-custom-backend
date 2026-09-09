using Microsoft.EntityFrameworkCore;
using NextTech.Application.External;
using NextTech.Application.Interfaces;
using NextTech.Infrastructure.Persistence.Oracle.Models;

namespace NextTech.Infrastructure.Persistence.Oracle.Repositories;

public sealed class CompradorCentralReader(OracleDbContext dbContext)
    : ICompradorCentralReader
{
    public Task<CompradorCentralDto?> ObtenerPorIdAsync(
        long idUsuario,
        CancellationToken cancellationToken = default)
        => Proyectar(dbContext.Usuarios.AsNoTracking().Where(x => x.IdUsuario == idUsuario))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<CompradorCentralDto?> ObtenerPorNicknameAsync(
        string nickname,
        CancellationToken cancellationToken = default)
        => Proyectar(dbContext.Usuarios.AsNoTracking().Where(x => x.Nickname == nickname))
            .SingleOrDefaultAsync(cancellationToken);

    public Task<CompradorCentralDto?> ObtenerPorCorreoAsync(
        string correo,
        CancellationToken cancellationToken = default)
        => Proyectar(dbContext.Usuarios.AsNoTracking().Where(x => x.Correo == correo))
            .SingleOrDefaultAsync(cancellationToken);

    private static IQueryable<CompradorCentralDto> Proyectar(
        IQueryable<UsuarioCentral> query)
        => query.Select(x => new CompradorCentralDto(
            x.IdUsuario,
            x.Correo,
            x.Telefono,
            x.FechaNacimiento,
            x.Nickname,
            x.NotificaEmail == 1,
            x.NotificaWhatsApp == 1));
}
