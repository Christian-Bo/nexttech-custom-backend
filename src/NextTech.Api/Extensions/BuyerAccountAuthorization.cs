using Microsoft.AspNetCore.Authorization;
using NextTech.Application.Authentication;
using NextTech.Application.Common.CurrentActor;
using NextTech.Application.Interfaces;

namespace NextTech.Api.Extensions;

public sealed class BuyerAccountRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Verifica contra Oracle que el comprador representado por el JWT siga existiendo,
/// activo y no bloqueado. Oracle es la fuente de verdad de la identidad del comprador.
/// </summary>
public sealed class BuyerAccountAuthorizationHandler(
    ICentralIdentityGateway identity,
    IHttpContextAccessor httpContextAccessor)
    : AuthorizationHandler<BuyerAccountRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BuyerAccountRequirement requirement)
    {
        if (!string.Equals(
                context.User.FindFirst(ActorClaims.ActorType)?.Value,
                ActorTypes.Buyer,
                StringComparison.Ordinal))
        {
            return;
        }

        if (!long.TryParse(context.User.FindFirst(ActorClaims.BuyerId)?.Value, out var buyerId) || buyerId <= 0)
            return;

        var ct = httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var buyer = await identity.FindByIdAsync(buyerId, ct);

        if (buyer is null || !buyer.Activo || buyer.Bloqueado)
            return;

        context.Succeed(requirement);
    }
}
