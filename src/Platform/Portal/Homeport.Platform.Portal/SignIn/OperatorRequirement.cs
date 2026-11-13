// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Portal.SignIn;

/// <summary>The signed-in user carries the claim of <see cref="SignInOptions"/> that grants access.</summary>
internal sealed class OperatorRequirement : IAuthorizationRequirement;

/// <summary>
/// Checks <see cref="OperatorRequirement"/>. The claim may hold several
/// values, as a list of roles does; one of them must match. A signed-in user
/// without it is forbidden, which the portal answers with the
/// <em>access denied</em> page.
/// </summary>
internal sealed class OperatorRequirementHandler(IOptions<SignInOptions> options, ILogger<OperatorRequirementHandler> logger)
    : AuthorizationHandler<OperatorRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, OperatorRequirement requirement)
    {
        // An anonymous request fails the authenticated-user requirement and is challenged.
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var signIn = options.Value;
        if (!context.User.HasClaim(signIn.ClaimType, signIn.ClaimValue))
        {
            logger.LogWarning("A signed-in user without the claim {ClaimType} = {ClaimValue} was refused", signIn.ClaimType, signIn.ClaimValue);
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
