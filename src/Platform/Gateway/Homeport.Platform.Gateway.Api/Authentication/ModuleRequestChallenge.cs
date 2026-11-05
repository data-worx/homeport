// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Routing;
using Microsoft.AspNetCore.Authentication;

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>
/// Challenges every request under <c>/modules/</c> that carries no valid
/// client token, before a routing outcome reaches the caller: a <c>404</c>
/// for an undeclared path or a <c>405</c> with <c>Allow</c> for an undeclared
/// method would tell an anonymous caller which paths a module serves and
/// which methods each accepts (#119).
/// </summary>
internal sealed class ModuleRequestChallenge(RequestDelegate next, ILogger<ModuleRequestChallenge> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.StartsWithSegments(ModuleRoutes.ModulesPrefix)
            && context.User.Identity?.IsAuthenticated != true)
        {
            logger.LogDebug("A module request without a valid client token was challenged before routing");
            await context.ChallengeAsync();

            return;
        }

        await next(context);
    }
}
