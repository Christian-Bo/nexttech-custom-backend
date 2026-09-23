using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using NextTech.Application.Authentication;
using NextTech.Application.Interfaces;

namespace NextTech.Api.Extensions;

public sealed record InternalAccountRequirement(
    bool AllowPasswordChangeRequired,
    IReadOnlySet<string>? AllowedRoles = null) : IAuthorizationRequirement;

public sealed class InternalAccountAuthorizationHandler(IInternalAuthRepository repository)
    : AuthorizationHandler<InternalAccountRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        InternalAccountRequirement requirement)
    {
        if (!string.Equals(
                context.User.FindFirstValue("actor_type"),
                ActorTypes.Internal,
                StringComparison.Ordinal))
        {
            return;
        }

        if (!int.TryParse(context.User.FindFirstValue("internal_user_id"), out var userId))
            return;

        var user = await repository.FindByIdAsync(userId, CancellationToken.None);
        if (user is null || !user.Activo)
            return;

        if (user.BloqueadoHasta is not null && user.BloqueadoHasta > DateTime.UtcNow)
            return;

        // Role/password-state changes invalidate previously issued internal JWTs.
        // This prevents an old token from gaining permissions after an administrative change.
        var tokenRole = context.User.FindFirstValue(ClaimTypes.Role);
        if (!string.Equals(tokenRole, user.Role, StringComparison.Ordinal))
            return;

        var mustChangeClaim = context.User.FindFirstValue("must_change_password");
        if (!bool.TryParse(mustChangeClaim, out var tokenMustChange) ||
            tokenMustChange != user.DebeCambiarPassword)
        {
            return;
        }

        if (!requirement.AllowPasswordChangeRequired && user.DebeCambiarPassword)
            return;

        if (requirement.AllowedRoles is not null && !requirement.AllowedRoles.Contains(user.Role))
            return;

        context.Succeed(requirement);
    }
}
