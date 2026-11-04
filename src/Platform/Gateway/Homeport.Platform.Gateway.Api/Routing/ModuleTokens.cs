// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using Homeport.Platform.Gateway.Api.Authentication;
using Homeport.Platform.Gateway.Infrastructure.Signing;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>
/// Replaces the client's token with a module token on every request forwarded
/// to a module (ADR-0011). The client's token never reaches the module.
/// </summary>
internal sealed class ModuleTokens(ModuleTokenIssuer issuer, IOptions<ClientAuthenticationOptions> options) : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        if (context.Route.Metadata?.TryGetValue(ModuleRoutes.ModuleIdMetadata, out var moduleId) == true)
        {
            context.AddRequestTransform(transform => ReplaceAsync(transform, moduleId));
        }
    }

    /// <summary>Sets a module token for <paramref name="moduleId"/> in place of the client's token.</summary>
    internal ValueTask ReplaceAsync(RequestTransformContext context, string moduleId)
    {
        // The client policy of every module route has set it.
        var client = context.HttpContext.Features.Get<ClientResponse>()
            ?? throw new InvalidOperationException("A request reached a module without a known client.");
        var user = context.HttpContext.User;

        // A client-credentials token without a subject stands for the client itself (RFC 9068).
        var subject = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? client.ClientId;

        var token = issuer.Issue(new ModuleCaller(
            moduleId,
            client.TenantId,
            client.ClientId,
            subject,
            ClientScopes.Of(user, options.Value.ScopeClaim)));
        context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        return ValueTask.CompletedTask;
    }
}
