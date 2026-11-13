// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using Homeport.Platform.Gateway.Api.Admin;
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
/// to a module (ADR-0011), and with a caller token of the same claims on every
/// request forwarded to a platform service (ADR-0019), and the operator's
/// token with a caller token of scope <c>admin</c> on every request forwarded
/// to an admin endpoint (ADR-0020). Neither token ever leaves the gateway.
/// </summary>
internal sealed class ModuleTokens(
    ModuleTokenIssuer issuer,
    IOptions<ClientAuthenticationOptions> options,
    IOptions<AdminApiOptions> admin,
    ILogger<ModuleTokens> logger)
    : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        if (context.Route.Metadata?.TryGetValue(AdminRoutes.AdminAudienceMetadata, out var service) == true)
        {
            context.AddRequestTransform(transform => ReplaceForOperatorAsync(transform, service));
            return;
        }

        if (context.Route.Metadata?.TryGetValue(ModuleRoutes.ModuleIdMetadata, out var audience) == true
            || context.Route.Metadata?.TryGetValue(PlatformRoutes.AudienceMetadata, out audience) == true)
        {
            context.AddRequestTransform(transform => ReplaceAsync(transform, audience));
        }
    }

    /// <summary>Sets a token for <paramref name="audience"/>, a module id or a platform service, in place of the client's token.</summary>
    internal ValueTask ReplaceAsync(RequestTransformContext context, string audience)
    {
        // The client policy of every module route has set it.
        var client = context.HttpContext.Features.Get<ClientResponse>()
            ?? throw new InvalidOperationException("A request reached a module without a known client.");
        var user = context.HttpContext.User;

        // A client-credentials token without a subject stands for the client itself (RFC 9068).
        var subject = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? client.ClientId;

        var token = issuer.Issue(new ModuleCaller(
            audience,
            client.TenantId,
            client.ClientId,
            subject,
            ClientScopes.Of(user, options.Value.ScopeClaim)));
        context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        return ValueTask.CompletedTask;
    }

    /// <summary>Sets the operator's caller token for <paramref name="audience"/>, a platform service, in place of the operator's token.</summary>
    internal ValueTask ReplaceForOperatorAsync(RequestTransformContext context, string audience)
    {
        // The admin policy of every admin route has checked that it is there.
        var user = context.HttpContext.User;
        var subject = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
            ?? throw new InvalidOperationException("A request reached an admin endpoint without the operator's subject.");

        var name = user.FindFirst(admin.Value.NameClaim)?.Value;
        if (string.IsNullOrWhiteSpace(name))
        {
            logger.LogDebug("The operator's token carries no {NameClaim}; the subject stands in for the name", admin.Value.NameClaim);
            name = subject;
        }

        var token = issuer.Issue(new OperatorCaller(audience, subject, name));
        context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

        return ValueTask.CompletedTask;
    }
}
