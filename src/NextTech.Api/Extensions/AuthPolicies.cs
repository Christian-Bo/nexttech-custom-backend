using NextTech.Application.Authentication;

namespace NextTech.Api.Extensions;

public static class AuthPolicies
{
    public static IServiceCollection AddNextTechAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("BuyerOnly", p => p.RequireClaim("actor_type", ActorTypes.Buyer));
            options.AddPolicy("InternalAuthenticated", p => p.RequireClaim("actor_type", ActorTypes.Internal));
            options.AddPolicy("InternalOnly", p => p.RequireClaim("actor_type", ActorTypes.Internal).RequireClaim("must_change_password", "false"));
            options.AddPolicy("AdminOnly", p => p.RequireRole(InternalRoles.Admin).RequireClaim("actor_type", ActorTypes.Internal).RequireClaim("must_change_password", "false"));
            options.AddPolicy("SupervisorOnly", p => p.RequireRole(InternalRoles.Supervisor).RequireClaim("actor_type", ActorTypes.Internal).RequireClaim("must_change_password", "false"));
            options.AddPolicy("DeliveryDriverOnly", p => p.RequireRole(InternalRoles.DeliveryDriver).RequireClaim("actor_type", ActorTypes.Internal).RequireClaim("must_change_password", "false"));
        });
        return services;
    }
}
