// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Homeport.Platform.Gateway.Api.Admin;

/// <summary>
/// Checks that the operator's token carries a subject and the admin claim.
/// A token without either fails the requirement, which the gateway answers
/// with <c>403</c>. The claim may hold several values, as a list of roles
/// does; one of them must match.
/// </summary>
internal sealed class AdminClaimHandler(IOptions<AdminApiOptions> options, ILogger<AdminClaimHandler> logger)
    : AuthorizationHandler<AdminClaimRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminClaimRequirement requirement)
    {
        // An anonymous request fails the authenticated-user requirement and is challenged.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var admin = options.Value;
        if (!context.User.HasClaim(admin.ClaimType, admin.ClaimValue))
        {
            logger.LogWarning("An operator token without the claim {ClaimType} = {ClaimValue} was refused", admin.ClaimType, admin.ClaimValue);
            context.Fail();
            return Task.CompletedTask;
        }

        // The caller token names the operator by it; the audit needs it.
        if (string.IsNullOrWhiteSpace(context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value))
        {
            logger.LogWarning("An operator token without a subject was refused");
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
