// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Routing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Model;

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>
/// Checks that the client's token grants the scope the matched route declares.
/// A missing scope fails the requirement, which the gateway answers with
/// <c>403</c>; the request is marked with <see cref="MissingScope"/>.
/// </summary>
internal sealed class RouteScopeHandler(
    IOptions<ClientAuthenticationOptions> options,
    ILogger<RouteScopeHandler> logger)
    : AuthorizationHandler<RouteScopeRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RouteScopeRequirement requirement)
    {
        // An anonymous request fails the authenticated-user requirement and is challenged.
        if (context.User.Identity?.IsAuthenticated != true || context.Resource is not HttpContext httpContext)
        {
            return Task.CompletedTask;
        }

        var metadata = httpContext.GetEndpoint()?.Metadata.GetMetadata<RouteModel>()?.Config.Metadata;
        if (metadata?.TryGetValue(ModuleRoutes.ScopeMetadata, out var scope) != true || string.IsNullOrEmpty(scope))
        {
            logger.LogWarning("Route {Path} declares no scope; the request was refused", httpContext.Request.Path);
            context.Fail();
            return Task.CompletedTask;
        }

        if (!ClientScopes.Of(context.User, options.Value.ScopeClaim).Contains(scope, StringComparer.Ordinal))
        {
            logger.LogWarning("A client token without the scope {Scope} of route {Path} was refused", scope, httpContext.Request.Path);
            httpContext.Features.Set(new MissingScope(scope));
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
