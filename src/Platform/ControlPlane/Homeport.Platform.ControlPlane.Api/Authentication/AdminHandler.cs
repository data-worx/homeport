// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Homeport.Platform.ControlPlane.Application;
using Homeport.Shared.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Homeport.Platform.ControlPlane.Api.Authentication;

/// <summary>
/// Checks <see cref="AdminRequirement"/> on the gateway's token. A failure is
/// answered with <c>403</c>.
/// </summary>
internal sealed class AdminHandler(ILogger<AdminHandler> logger) : AuthorizationHandler<AdminRequirement>
{
    /// <summary>
    /// The operator a caller token names, for the audit entry. Without a
    /// display name, the subject stands in for it.
    /// </summary>
    internal static Operator OperatorOf(ClaimsPrincipal user)
    {
        // The policy has made sure the token names one.
        var subject = user.FindFirst(GatewayTokens.SubjectClaim)?.Value
            ?? throw new InvalidOperationException("An admin request reached the controller without a subject.");

        return new Operator(subject, user.FindFirst(GatewayTokens.NameClaim)?.Value ?? subject);
    }

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
