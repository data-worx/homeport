// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace Homeport.Shared.Internal.Authorization;

/// <summary>
/// Checks <see cref="AdminRequirement"/> on the gateway's token. A failure is
/// answered with <c>403</c>.
/// </summary>
internal sealed class AdminHandler(ILogger<AdminHandler> logger) : AuthorizationHandler<AdminRequirement>
{
    /// <summary>Succeeds for a caller token with the admin scope and no tenant.</summary>
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
    {
        // An anonymous request fails the authenticated-user requirement and is challenged.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(claim => claim.Type == GatewayTokens.TenantIdClaim))
        {
            logger.LogWarning("A caller token with a tenant was refused for an admin endpoint");
            context.Fail();
            return Task.CompletedTask;
        }

        var scopes = context.User.FindAll(GatewayTokens.ScopeClaim)
            .SelectMany(claim => claim.Value.Split(GatewayTokens.ScopeSeparator, StringSplitOptions.RemoveEmptyEntries));
        if (!scopes.Contains(GatewayTokens.AdminScope, StringComparer.Ordinal))
        {
            logger.LogWarning("A token without the scope {Scope} was refused for an admin endpoint", GatewayTokens.AdminScope);
            context.Fail();
            return Task.CompletedTask;
        }

        if (!context.User.HasClaim(claim => claim.Type == GatewayTokens.SubjectClaim))
        {
            logger.LogWarning("An admin token without a subject was refused");
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
