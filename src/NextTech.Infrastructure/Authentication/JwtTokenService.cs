using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NextTech.Application.Authentication;
using NextTech.Application.Interfaces;

namespace NextTech.Infrastructure.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokenResult CreateBuyerToken(BuyerProfile buyer)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, buyer.IdUsuario.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("actor_type", ActorTypes.Buyer),
            new Claim("buyer_id", buyer.IdUsuario.ToString()),
            new Claim("nickname", buyer.Nickname)
        };
        return Create(claims, ActorTypes.Buyer, false);
    }

    public AccessTokenResult CreateInternalToken(InternalUserAuthRecord user)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.IdUsuarioInterno.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new Claim("actor_type", ActorTypes.Internal),
            new Claim("internal_user_id", user.IdUsuarioInterno.ToString()),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("must_change_password", user.DebeCambiarPassword ? "true" : "false")
        };
        return Create(claims, ActorTypes.Internal, user.DebeCambiarPassword);
    }

    private AccessTokenResult Create(IEnumerable<Claim> claims, string actorType, bool mustChangePassword)
    {
        if (_options.Key.Length < 32)
            throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres.");

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_options.ExpirationMinutes);
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key)),
            SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new AccessTokenResult(
            new JwtSecurityTokenHandler().WriteToken(jwt),
            expires,
            actorType,
            mustChangePassword);
    }
}
