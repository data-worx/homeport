// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Shared.Internal.Authorization;

/// <summary>
/// The policies a platform service holds the gateway's tokens to: the
/// gateway's own service token (ADR-0014) and the operator's caller token
/// (ADR-0020). Both expect the gateway's token authentication as the default
/// scheme, or a policy that names it.
/// </summary>
public static class PlatformPolicies
{
    /// <summary>Only the gateway's service token: subject <c>homeport-gateway</c>, no tenant, no scope.</summary>
    public const string GatewayService = "gateway-service";

    /// <summary>Only the operator's caller token: scope <c>admin</c>, a subject, no tenant.</summary>
    public const string Admin = "admin";

    /// <summary>
    /// Adds authorization with the policies <see cref="GatewayService"/> and
    /// <see cref="Admin"/>. A token that fails one is answered with <c>403</c>.
    /// </summary>
    /// <param name="services">The service's services.</param>
    /// <param name="authenticationScheme">
    /// The scheme the policies authenticate with; <c>null</c> for the default scheme.
    /// </param>
    public static AuthorizationBuilder AddPlatformPolicies(this IServiceCollection services, string? authenticationScheme = null)
    {
        services.AddSingleton<IAuthorizationHandler, GatewayServiceHandler>();
        services.AddSingleton<IAuthorizationHandler, AdminHandler>();

        return services.AddAuthorizationBuilder()
            .AddPolicy(GatewayService, policy => Require(policy, authenticationScheme).AddRequirements(new GatewayServiceRequirement()))
            .AddPolicy(Admin, policy => Require(policy, authenticationScheme).AddRequirements(new AdminRequirement()));
    }

    /// <summary>An authenticated user, of <paramref name="authenticationScheme"/> where one is named.</summary>
    private static AuthorizationPolicyBuilder Require(AuthorizationPolicyBuilder policy, string? authenticationScheme)
    {
        if (authenticationScheme is not null)
        {
            policy.AddAuthenticationSchemes(authenticationScheme);
        }

        return policy.RequireAuthenticatedUser();
    }
}
