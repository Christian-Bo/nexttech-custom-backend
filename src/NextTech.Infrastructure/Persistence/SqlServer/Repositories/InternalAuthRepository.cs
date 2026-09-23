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

    public Task<InternalUserInfo?> FindProfileByIdAsync(int userId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return (from user in db.UsuarioInterno.AsNoTracking()
                join role in db.Rol.AsNoTracking() on user.IdRol equals role.IdRol
                where user.IdUsuarioInterno == userId
                select new InternalUserInfo(
                    user.IdUsuarioInterno,
                    user.Nombres,
                    user.Apellidos,
                    user.Nombres + " " + user.Apellidos,
                    user.Correo,
                    new InternalRoleInfo(role.IdRol, role.Codigo, role.Nombre),
                    user.Activo,
                    user.DebeCambiarPassword,
                    user.IntentosFallidos,
                    user.BloqueadoHasta,
                    user.BloqueadoHasta != null && user.BloqueadoHasta > now,
                    user.UltimoAcceso,
                    user.FechaCreacion,
                    user.FechaActualizacion,
                    user.FechaDesactivacion))
            .SingleOrDefaultAsync(ct);
    }

    public async Task RegisterUnknownFailedLoginAsync(string? ip, CancellationToken ct)
    {
        db.BitacoraAuditoria.Add(SystemAudit(
            "LOGIN_FALLIDO",
            "FALLIDO",
            null,
            ip,
            "Intento de autenticación interna con cuenta no registrada."));
        await db.SaveChangesAsync(ct);
    }

    public async Task RegisterRejectedLoginAsync(int userId, string reason, string? ip, CancellationToken ct)
    {
        db.BitacoraAuditoria.Add(SystemAudit(
            "LOGIN_RECHAZADO",
            "FALLIDO",
            userId,
            ip,
            reason));
        await db.SaveChangesAsync(ct);
    }

    public async Task RegisterFailedLoginAsync(
        int userId,
        int newFailedAttempts,
        DateTime? blockedUntil,
        string? ip,
        CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleAsync(x => x.IdUsuarioInterno == userId, ct);
        user.IntentosFallidos = newFailedAttempts;
        user.BloqueadoHasta = blockedUntil;
        user.FechaActualizacion = DateTime.UtcNow;

        var detail = blockedUntil is null
            ? $"Intento de autenticación interna fallido. Intentos acumulados: {newFailedAttempts}."
            : $"Cuenta bloqueada temporalmente después de {newFailedAttempts} intentos fallidos.";

        db.BitacoraAuditoria.Add(SystemAudit("LOGIN_FALLIDO", "FALLIDO", userId, ip, detail));
        await db.SaveChangesAsync(ct);
    }

    public async Task RegisterSuccessfulLoginAsync(int userId, string? ip, CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleAsync(x => x.IdUsuarioInterno == userId, ct);
        user.IntentosFallidos = 0;
        user.BloqueadoHasta = null;
        user.UltimoAcceso = DateTime.UtcNow;
        user.FechaActualizacion = DateTime.UtcNow;
        db.BitacoraAuditoria.Add(InternalAudit(userId, "LOGIN_EXITOSO", "EXITOSO", userId, ip, null));
        await db.SaveChangesAsync(ct);
    }

    public async Task UpgradePasswordHashAsync(int userId, string passwordHash, CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleAsync(x => x.IdUsuarioInterno == userId, ct);
        user.PasswordHash = passwordHash;
        user.FechaActualizacion = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(int userId, string passwordHash, string? ip, CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleAsync(x => x.IdUsuarioInterno == userId, ct);
        user.PasswordHash = passwordHash;
        user.DebeCambiarPassword = false;
        user.IntentosFallidos = 0;
        user.BloqueadoHasta = null;
        user.FechaActualizacion = DateTime.UtcNow;
        db.BitacoraAuditoria.Add(InternalAudit(userId, "CAMBIO_PASSWORD", "EXITOSO", userId, ip, null));
        await db.SaveChangesAsync(ct);
    }

    private static BitacoraAuditoria SystemAudit(
        string action,
        string result,
        int? targetUserId,
        string? ip,
        string? detail) => new()
        {
            IdTipoActor = AuditActorIds.System,
            IdUsuarioInterno = null,
            Accion = action,
            Entidad = "UsuarioInterno",
            IdEntidad = targetUserId,
            Resultado = result,
            DireccionIp = ip,
            Detalle = detail,
            FechaHora = DateTime.UtcNow
        };

    private static BitacoraAuditoria InternalAudit(
        int actorUserId,
        string action,
        string result,
        int? targetUserId,
        string? ip,
        string? detail) => new()
        {
            IdTipoActor = AuditActorIds.InternalUser,
            IdUsuarioInterno = actorUserId,
            Accion = action,
            Entidad = "UsuarioInterno",
            IdEntidad = targetUserId,
            Resultado = result,
            DireccionIp = ip,
            Detalle = detail,
            FechaHora = DateTime.UtcNow
        };
}
