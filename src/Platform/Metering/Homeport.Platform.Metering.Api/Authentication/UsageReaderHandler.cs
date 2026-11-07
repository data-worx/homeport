// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Homeport.Shared.Authentication;
using Homeport.Shared.Client.Platform.Metering.V1;
using Microsoft.AspNetCore.Authorization;

namespace Homeport.Platform.Metering.Api.Authentication;

/// <summary>
/// Checks <see cref="UsageReaderRequirement"/> on the gateway's token. A
/// failure is answered with <c>403</c>.
/// </summary>
internal sealed class UsageReaderHandler(ILogger<UsageReaderHandler> logger) : AuthorizationHandler<UsageReaderRequirement>
{
    /// <summary>The tenant the caller token names, or none.</summary>
    internal static Guid? TenantIdOf(ClaimsPrincipal user)
    {
        return Guid.TryParse(user.FindFirst(GatewayTokens.TenantIdClaim)?.Value, out var tenantId) ? tenantId : null;
    }

    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UsageReaderRequirement requirement)
    {
        // An anonymous request fails the authenticated-user requirement and is challenged.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (TenantIdOf(context.User) is null)
        {
            logger.LogWarning("A gateway token without a tenant was refused for the usage export");
            context.Fail();
            return Task.CompletedTask;
        }

        var scopes = context.User.FindAll(GatewayTokens.ScopeClaim)
            .SelectMany(claim => claim.Value.Split(GatewayTokens.ScopeSeparator, StringSplitOptions.RemoveEmptyEntries));
        if (!scopes.Contains(UsageApi.ReadScope, StringComparer.Ordinal))
        {
            logger.LogWarning("A caller token without the scope {Scope} was refused for the usage export", UsageApi.ReadScope);
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
