using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using NextTech.Application.Authentication;
using NextTech.Application.Common.CurrentActor;
using NextTech.Domain.Exceptions;

namespace NextTech.Api.CurrentActor;

public sealed class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private ClaimsPrincipal User => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public bool IsAuthenticated => User.Identity?.IsAuthenticated == true;

    public bool EsComprador =>
        string.Equals(
            User.FindFirstValue(ActorClaims.ActorType),
            ActorTypes.Buyer,
            StringComparison.OrdinalIgnoreCase);

    public bool EsInterno =>
        string.Equals(
            User.FindFirstValue(ActorClaims.ActorType),
            ActorTypes.Internal,
            StringComparison.OrdinalIgnoreCase);

    public string? Rol => User.FindFirstValue(ClaimTypes.Role);

    public string? Email => User.FindFirstValue(ClaimTypes.Email);

    public string? Nickname => User.FindFirstValue(ActorClaims.Nickname);

    public string? Telefono => User.FindFirstValue(ActorClaims.Phone);

    public long RequireIdCompradorExterno()
    {
        var raw = User.FindFirstValue(ActorClaims.BuyerId)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!EsComprador || !long.TryParse(raw, out var id))
        {
            throw new ForbiddenException("Se requiere un comprador autenticado.");
        }

        return id;
    }

    public int RequireIdUsuarioInterno()
    {
        var raw = User.FindFirstValue(ActorClaims.InternalUserId)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!EsInterno || !int.TryParse(raw, out var id))
        {
            throw new ForbiddenException("Se requiere un usuario interno autenticado.");
        }

        return id;
    }
}
