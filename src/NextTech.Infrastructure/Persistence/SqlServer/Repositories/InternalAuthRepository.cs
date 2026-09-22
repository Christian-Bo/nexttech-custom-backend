using Microsoft.EntityFrameworkCore;
using NextTech.Application.Authentication;
using NextTech.Application.Interfaces;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Repositories;

public sealed class InternalAuthRepository(NextTechDbContext db) : IInternalAuthRepository
{
    public Task<InternalUserAuthRecord?> FindByEmailAsync(string normalizedEmail, CancellationToken ct)
        => (from user in db.UsuarioInterno.AsNoTracking()
            join role in db.Rol.AsNoTracking() on user.IdRol equals role.IdRol
            where user.Correo.ToLower() == normalizedEmail
            select new InternalUserAuthRecord(
                user.IdUsuarioInterno,
                user.Correo,
                user.PasswordHash,
                role.Codigo,
                user.Activo,
                user.DebeCambiarPassword,
                user.IntentosFallidos,
                user.BloqueadoHasta))
           .SingleOrDefaultAsync(ct);

    public Task<InternalUserAuthRecord?> FindByIdAsync(int userId, CancellationToken ct)
        => (from user in db.UsuarioInterno.AsNoTracking()
            join role in db.Rol.AsNoTracking() on user.IdRol equals role.IdRol
            where user.IdUsuarioInterno == userId
            select new InternalUserAuthRecord(
                user.IdUsuarioInterno,
                user.Correo,
                user.PasswordHash,
                role.Codigo,
                user.Activo,
                user.DebeCambiarPassword,
                user.IntentosFallidos,
                user.BloqueadoHasta))
           .SingleOrDefaultAsync(ct);

    public async Task RegisterFailedLoginAsync(int userId, int newFailedAttempts, DateTime? blockedUntil, string? ip, CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleAsync(x => x.IdUsuarioInterno == userId, ct);
        user.IntentosFallidos = newFailedAttempts;
        user.BloqueadoHasta = blockedUntil;
        user.FechaActualizacion = DateTime.UtcNow;
        db.BitacoraAuditoria.Add(Audit(userId, "LOGIN_FALLIDO", "FALLIDO", ip, "Intento de autenticación interna fallido."));
        await db.SaveChangesAsync(ct);
    }

    public async Task RegisterSuccessfulLoginAsync(int userId, string? ip, CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleAsync(x => x.IdUsuarioInterno == userId, ct);
        user.IntentosFallidos = 0;
        user.BloqueadoHasta = null;
        user.UltimoAcceso = DateTime.UtcNow;
        user.FechaActualizacion = DateTime.UtcNow;
        db.BitacoraAuditoria.Add(Audit(userId, "LOGIN_EXITOSO", "EXITOSO", ip, null));
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(int userId, string passwordHash, CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleAsync(x => x.IdUsuarioInterno == userId, ct);
        user.PasswordHash = passwordHash;
        user.DebeCambiarPassword = false;
        user.IntentosFallidos = 0;
        user.BloqueadoHasta = null;
        user.FechaActualizacion = DateTime.UtcNow;
        db.BitacoraAuditoria.Add(Audit(userId, "CAMBIO_PASSWORD", "EXITOSO", null, null));
        await db.SaveChangesAsync(ct);
    }

    private static BitacoraAuditoria Audit(int userId, string action, string result, string? ip, string? detail) => new()
    {
        IdTipoActor = 2,
        IdUsuarioInterno = userId,
        Accion = action,
        Entidad = "UsuarioInterno",
        IdEntidad = userId,
        Resultado = result,
        DireccionIp = ip,
        Detalle = detail,
        FechaHora = DateTime.UtcNow
    };
}
