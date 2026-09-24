using System.Security.Claims;
using NextTech.Application.Authentication;

namespace NextTech.Application.Common.CurrentActor;

/// <summary>
/// Nombres de claims que emite Auth. La tienda solo los lee.
/// </summary>
public static class ActorClaims
{
    public const string ActorType = "actor_type";
    public const string BuyerId = "buyer_id";
    public const string InternalUserId = "internal_user_id";
    public const string Nickname = "nickname";
    public const string Phone = "phone";
    public const string MustChangePassword = "must_change_password";

    public static string Comprador => ActorTypes.Buyer;

    public static string Interno => ActorTypes.Internal;

    public static string NameIdentifier => ClaimTypes.NameIdentifier;

    public static string Role => ClaimTypes.Role;

    public static string Email => ClaimTypes.Email;
}
