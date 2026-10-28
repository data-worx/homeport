// SPDX-License-Identifier: AGPL-3.0-or-later

using Yarp.ReverseProxy.Transforms;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>
/// The <c>X-Homeport-*</c> headers, which only the gateway may set. A caller
/// that sends one could otherwise claim a tenant or a client to the module.
/// </summary>
internal static class HomeportHeaders
{
    /// <summary>The prefix every Homeport header starts with.</summary>
    internal const string Prefix = "X-Homeport-";

    /// <summary>Removes every incoming Homeport header from the forwarded request.</summary>
    internal static ValueTask StripAsync(RequestTransformContext context)
    {
        var incoming = context.ProxyRequest.Headers
            .Select(header => header.Key)
            .Where(name => name.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var name in incoming)
        {
            context.ProxyRequest.Headers.Remove(name);
        }

        return ValueTask.CompletedTask;
    }
}
