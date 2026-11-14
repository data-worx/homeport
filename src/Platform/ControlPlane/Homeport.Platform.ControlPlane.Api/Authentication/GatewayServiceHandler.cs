// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Homeport.Platform.ControlPlane.Api.Authentication;

/// <summary>
/// Checks <see cref="GatewayServiceRequirement"/> on the gateway's token. A
/// failure is answered with <c>403</c>.
/// </summary>
internal sealed class GatewayServiceHandler(ILogger<GatewayServiceHandler> logger) : AuthorizationHandler<GatewayServiceRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, GatewayServiceRequirement requirement)
    {
        // An anonymous request fails the authenticated-user requirement and is challenged.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (context.User.FindFirst(GatewayTokens.SubjectClaim)?.Value != GatewayTokens.GatewaySubject)
        {
            logger.LogWarning("A token whose subject is not the gateway was refused for a service endpoint");
            context.Fail();
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(claim => claim.Type == GatewayTokens.TenantIdClaim))
        {
            logger.LogWarning("A token with a tenant was refused for a service endpoint");
            context.Fail();
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(claim => claim.Type == GatewayTokens.ScopeClaim))
        {
            logger.LogWarning("A token with a scope was refused for a service endpoint");
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
