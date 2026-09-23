using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NextTech.Application.Authentication;
using NextTech.Application.Common;
using NextTech.Application.Interfaces;
using NextTech.Infrastructure.Persistence.SqlServer.Entities;

namespace NextTech.Infrastructure.Persistence.SqlServer.Repositories;

public sealed class InternalUserAdministrationRepository(NextTechDbContext db)
    : IInternalUserAdministrationRepository
{
    public async Task<IReadOnlyList<InternalRoleInfo>> GetRolesAsync(CancellationToken ct)
        => await db.Rol
            .AsNoTracking()
            .OrderBy(x => x.IdRol)
            .Select(x => new InternalRoleInfo(x.IdRol, x.Codigo, x.Nombre))
            .ToListAsync(ct);

    public Task<InternalRoleInfo?> FindRoleByCodeAsync(string roleCode, CancellationToken ct)
        => db.Rol
            .AsNoTracking()
            .Where(x => x.Codigo == roleCode)
            .Select(x => new InternalRoleInfo(x.IdRol, x.Codigo, x.Nombre))
            .SingleOrDefaultAsync(ct);

    public Task<InternalUserInfo?> FindByIdAsync(int userId, CancellationToken ct)
        => ProjectByIdAsync(userId, ct);

    public Task<bool> EmailExistsAsync(string normalizedEmail, int? excludingUserId, CancellationToken ct)
        => db.UsuarioInterno
            .AsNoTracking()
            .AnyAsync(
                x => x.Correo.ToLower() == normalizedEmail &&
                     (!excludingUserId.HasValue || x.IdUsuarioInterno != excludingUserId.Value),
                ct);

    public Task<int> CountActiveAdminsAsync(CancellationToken ct)
        => (from user in db.UsuarioInterno.AsNoTracking()
            join role in db.Rol.AsNoTracking() on user.IdRol equals role.IdRol
            where user.Activo && role.Codigo == InternalRoles.Admin
            select user.IdUsuarioInterno)
            .CountAsync(ct);

    public async Task<PagedResult<InternalUserInfo>> SearchAsync(
        InternalUserListRequest request,
        CancellationToken ct)
    {
        var query = from user in db.UsuarioInterno.AsNoTracking()
                    join role in db.Rol.AsNoTracking() on user.IdRol equals role.IdRol
                    select new { User = user, Role = role };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x =>
                x.User.Nombres.Contains(search) ||
                x.User.Apellidos.Contains(search) ||
                x.User.Correo.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(request.RoleCode))
            query = query.Where(x => x.Role.Codigo == request.RoleCode);

        if (request.IsActive.HasValue)
            query = query.Where(x => x.User.Activo == request.IsActive.Value);

        var now = DateTime.UtcNow;
        if (request.IsLocked.HasValue)
        {
            query = request.IsLocked.Value
                ? query.Where(x => x.User.BloqueadoHasta != null && x.User.BloqueadoHasta > now)
                : query.Where(x => x.User.BloqueadoHasta == null || x.User.BloqueadoHasta <= now);
        }

        var totalItems = await query.CountAsync(ct);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)request.PageSize);

        var items = await query
            .OrderBy(x => x.User.Nombres)
            .ThenBy(x => x.User.Apellidos)
            .ThenBy(x => x.User.IdUsuarioInterno)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new InternalUserInfo(
                x.User.IdUsuarioInterno,
                x.User.Nombres,
                x.User.Apellidos,
                x.User.Nombres + " " + x.User.Apellidos,
                x.User.Correo,
                new InternalRoleInfo(x.Role.IdRol, x.Role.Codigo, x.Role.Nombre),
                x.User.Activo,
                x.User.DebeCambiarPassword,
                x.User.IntentosFallidos,
                x.User.BloqueadoHasta,
                x.User.BloqueadoHasta != null && x.User.BloqueadoHasta > now,
                x.User.UltimoAcceso,
                x.User.FechaCreacion,
                x.User.FechaActualizacion,
                x.User.FechaDesactivacion))
            .ToListAsync(ct);

        return new PagedResult<InternalUserInfo>(items, request.Page, request.PageSize, totalItems, totalPages);
    }

    public async Task<InternalUserInfo> CreateAsync(
        CreateInternalUserData data,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        var now = DateTime.UtcNow;
        var user = new UsuarioInterno
        {
            IdRol = data.RoleId,
            Nombres = data.FirstName,
            Apellidos = data.LastName,
            Correo = data.Email,
            PasswordHash = data.PasswordHash,
            Activo = true,
            DebeCambiarPassword = true,
            IntentosFallidos = 0,
            BloqueadoHasta = null,
            UltimoAcceso = null,
            FechaCreacion = now,
            FechaActualizacion = null,
            FechaDesactivacion = null
        };

        try
        {
            db.UsuarioInterno.Add(user);
            await db.SaveChangesAsync(ct);

            db.BitacoraAuditoria.Add(Audit(
                actorUserId,
                "USUARIO_INTERNO_CREADO",
                user.IdUsuarioInterno,
                ip,
                $"Usuario interno creado con rol IdRol={data.RoleId}."));
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await transaction.RollbackAsync(ct);
            throw new AppConflictException("Ya existe un usuario interno con ese correo.");
        }

        return await RequireByIdAsync(user.IdUsuarioInterno, ct);
    }

    public async Task<InternalUserInfo> UpdateAsync(
        int userId,
        UpdateInternalUserData data,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleOrDefaultAsync(x => x.IdUsuarioInterno == userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        user.Nombres = data.FirstName;
        user.Apellidos = data.LastName;
        user.Correo = data.Email;
        user.IdRol = data.RoleId;
        user.FechaActualizacion = DateTime.UtcNow;

        db.BitacoraAuditoria.Add(Audit(
            actorUserId,
            "USUARIO_INTERNO_ACTUALIZADO",
            userId,
            ip,
            $"Datos de usuario interno actualizados. IdRol={data.RoleId}."));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            throw new AppConflictException("Ya existe un usuario interno con ese correo.");
        }

        return await RequireByIdAsync(userId, ct);
    }

    public async Task<InternalUserInfo> SetActiveAsync(
        int userId,
        bool active,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleOrDefaultAsync(x => x.IdUsuarioInterno == userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        var now = DateTime.UtcNow;
        user.Activo = active;
        user.FechaDesactivacion = active ? null : now;
        user.FechaActualizacion = now;

        if (active)
        {
            user.IntentosFallidos = 0;
            user.BloqueadoHasta = null;
        }

        db.BitacoraAuditoria.Add(Audit(
            actorUserId,
            active ? "USUARIO_INTERNO_ACTIVADO" : "USUARIO_INTERNO_DESACTIVADO",
            userId,
            ip,
            null));

        await db.SaveChangesAsync(ct);
        return await RequireByIdAsync(userId, ct);
    }

    public async Task<InternalUserInfo> UnlockAsync(
        int userId,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleOrDefaultAsync(x => x.IdUsuarioInterno == userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        user.IntentosFallidos = 0;
        user.BloqueadoHasta = null;
        user.FechaActualizacion = DateTime.UtcNow;
        db.BitacoraAuditoria.Add(Audit(actorUserId, "USUARIO_INTERNO_DESBLOQUEADO", userId, ip, null));
        await db.SaveChangesAsync(ct);

        return await RequireByIdAsync(userId, ct);
    }

    public async Task<InternalUserInfo> ResetPasswordAsync(
        int userId,
        string passwordHash,
        int actorUserId,
        string? ip,
        CancellationToken ct)
    {
        var user = await db.UsuarioInterno.SingleOrDefaultAsync(x => x.IdUsuarioInterno == userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

        user.PasswordHash = passwordHash;
        user.DebeCambiarPassword = true;
        user.IntentosFallidos = 0;
        user.BloqueadoHasta = null;
        user.FechaActualizacion = DateTime.UtcNow;
        db.BitacoraAuditoria.Add(Audit(
            actorUserId,
            "PASSWORD_INTERNO_RESTABLECIDO",
            userId,
            ip,
            "Se estableció una contraseña temporal y se forzó cambio en el próximo acceso."));
        await db.SaveChangesAsync(ct);

        return await RequireByIdAsync(userId, ct);
    }

    private async Task<InternalUserInfo> RequireByIdAsync(int userId, CancellationToken ct)
        => await ProjectByIdAsync(userId, ct)
            ?? throw new AppNotFoundException("Usuario interno no encontrado.");

    private Task<InternalUserInfo?> ProjectByIdAsync(int userId, CancellationToken ct)
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

    private static BitacoraAuditoria Audit(
        int actorUserId,
        string action,
        int targetUserId,
        string? ip,
        string? detail) => new()
        {
            IdTipoActor = AuditActorIds.InternalUser,
            IdUsuarioInterno = actorUserId,
            Accion = action,
            Entidad = "UsuarioInterno",
            IdEntidad = targetUserId,
            Resultado = "EXITOSO",
            DireccionIp = ip,
            Detalle = detail,
            FechaHora = DateTime.UtcNow
        };

    private static bool IsUniqueViolation(DbUpdateException exception)
        => exception.InnerException is SqlException sql && sql.Number is 2601 or 2627;
}
