using Microsoft.EntityFrameworkCore;
using NextTech.Application.Authentication;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.Persistence.SqlServer.Repositories;

public sealed class InternalSecurityAuditRepository(NextTechDbContext db)
    : IInternalSecurityAuditRepository
{
    public async Task<PagedResult<InternalAuditEntryInfo>> SearchAsync(
        InternalAuditListRequest request,
        CancellationToken ct)
    {
        var internalActors =
            from user in db.UsuarioInterno.AsNoTracking()
            join role in db.Rol.AsNoTracking() on user.IdRol equals role.IdRol
            select new
            {
                UserId = (int?)user.IdUsuarioInterno,
                FullName = user.Nombres + " " + user.Apellidos,
                RoleCode = role.Codigo,
                RoleName = role.Nombre
            };

        var internalTargets = db.UsuarioInterno
            .AsNoTracking()
            .Select(user => new
            {
                UserId = (int?)user.IdUsuarioInterno,
                FullName = user.Nombres + " " + user.Apellidos
            });

        var query =
            from audit in db.BitacoraAuditoria.AsNoTracking()
            join actor in internalActors
                on audit.IdUsuarioInterno equals actor.UserId into actorRows
            from actor in actorRows.DefaultIfEmpty()
            join target in internalTargets
                on audit.IdEntidad equals target.UserId into targetRows
            from target in targetRows.DefaultIfEmpty()
            select new
            {
                Audit = audit,
                Actor = actor,
                Target = target
            };

        if (request.InternalUserId.HasValue)
            query = query.Where(x => x.Audit.IdUsuarioInterno == request.InternalUserId.Value);
        if (!string.IsNullOrWhiteSpace(request.Action))
            query = query.Where(x => x.Audit.Accion == request.Action);
        if (!string.IsNullOrWhiteSpace(request.Result))
            query = query.Where(x => x.Audit.Resultado == request.Result);
        if (request.FromUtc.HasValue)
            query = query.Where(x => x.Audit.FechaHora >= request.FromUtc.Value);
        if (request.ToUtc.HasValue)
            query = query.Where(x => x.Audit.FechaHora <= request.ToUtc.Value);

        var totalItems = await query.CountAsync(ct);
        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)request.PageSize);

        var items = await query
            .OrderByDescending(x => x.Audit.FechaHora)
            .ThenByDescending(x => x.Audit.IdBitacora)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new InternalAuditEntryInfo(
                x.Audit.IdBitacora,
                x.Audit.IdTipoActor == AuditActorIds.System
                    ? "SYSTEM"
                    : x.Audit.IdTipoActor == AuditActorIds.InternalUser
                        ? ActorTypes.Internal
                        : ActorTypes.Buyer,
                x.Audit.IdUsuarioInterno,
                x.Actor == null ? null : x.Actor.FullName,
                x.Actor == null ? null : x.Actor.RoleCode,
                x.Actor == null ? null : x.Actor.RoleName,
                x.Audit.IdCompradorExterno,
                x.Audit.Accion,
                x.Audit.Entidad,
                x.Audit.IdEntidad,
                x.Audit.Entidad == "UsuarioInterno" && x.Target != null
                    ? x.Target.FullName
                    : null,
                x.Audit.Resultado,
                x.Audit.DireccionIp,
                x.Audit.Detalle,
                x.Audit.FechaHora))
            .ToListAsync(ct);

        return new PagedResult<InternalAuditEntryInfo>(
            items,
            request.Page,
            request.PageSize,
            totalItems,
            totalPages);
    }
}
