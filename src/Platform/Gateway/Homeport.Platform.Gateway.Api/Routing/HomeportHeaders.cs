// SPDX-License-Identifier: AGPL-3.0-or-later

using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>
/// Strips the <c>X-Homeport-*</c> headers, which only the gateway may set, from
/// every forwarded request. A caller that sends one could otherwise claim a
/// tenant or a client to the module.
/// </summary>
internal sealed class HomeportHeaders(ILogger<HomeportHeaders> logger) : ITransformProvider
{
    /// <summary>The prefix every Homeport header starts with.</summary>
    internal const string Prefix = "X-Homeport-";

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        context.AddRequestTransform(StripAsync);
    }

    /// <summary>Removes every incoming Homeport header from the forwarded request.</summary>
    internal ValueTask StripAsync(RequestTransformContext context)
    {
        var incoming = context.ProxyRequest.Headers
            .Select(header => header.Key)
            .Where(name => name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var name in incoming)
        {
            context.ProxyRequest.Headers.Remove(name);
        }

        if (incoming.Count > 0)
        {
            logger.LogDebug("Stripped the incoming headers {HeaderNames}", incoming);
        }

        return ValueTask.CompletedTask;
    }
}
