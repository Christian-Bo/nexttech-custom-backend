using Microsoft.AspNetCore.Authorization;
using NextTech.Application.Authentication;

namespace NextTech.Api.Extensions;

public static class AuthPolicies
{
    public static IServiceCollection AddNextTechAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, BuyerAccountAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, InternalAccountAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("BuyerOnly", p => p.AddRequirements(new BuyerAccountRequirement()));
            options.AddPolicy("InternalAuthenticated", p => p.AddRequirements(
                new InternalAccountRequirement(AllowPasswordChangeRequired: true)));
            options.AddPolicy("InternalOnly", p => p.AddRequirements(
                new InternalAccountRequirement(AllowPasswordChangeRequired: false)));
            options.AddPolicy("AdminOnly", p => p.AddRequirements(
                new InternalAccountRequirement(false, Roles(InternalRoles.Admin))));
            options.AddPolicy("SupervisorOnly", p => p.AddRequirements(
                new InternalAccountRequirement(false, Roles(InternalRoles.Supervisor))));
            options.AddPolicy("DeliveryOnly", p => p.AddRequirements(
                new InternalAccountRequirement(false, Roles(InternalRoles.DeliveryDriver))));
            options.AddPolicy("DeliveryDriverOnly", p => p.AddRequirements(
                new InternalAccountRequirement(false, Roles(InternalRoles.DeliveryDriver))));
            options.AddPolicy("AdminOrSupervisor", p => p.AddRequirements(
                new InternalAccountRequirement(false, Roles(InternalRoles.Admin, InternalRoles.Supervisor))));
        });

        return services;
    }

    private static IReadOnlySet<string> Roles(params string[] roles)
        => new HashSet<string>(roles, StringComparer.Ordinal);
}
