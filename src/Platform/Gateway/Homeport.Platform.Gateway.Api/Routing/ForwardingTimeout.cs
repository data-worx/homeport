// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>
/// Gives every cluster the gateway builds - module, platform and admin alike -
/// the timeout of <see cref="ForwardingOptions"/>, in place of YARP's default
/// of 100 seconds. On expiry YARP reports <see cref="ForwarderError.RequestTimedOut"/>
/// with <c>504</c>, which the gateway answers with its timeout problem.
/// </summary>
/// <remarks>
/// It is YARP's activity timeout: it runs out when the request stays idle that
/// long, so a response that keeps streaming, such as a large export, is not cut
/// off, while a service that stops answering is.
/// </remarks>
internal sealed class ForwardingTimeout(IOptions<ForwardingOptions> options) : IProxyConfigFilter
{
    /// <summary>Sets the timeout on the cluster's forwarded requests.</summary>
    public ValueTask<ClusterConfig> ConfigureClusterAsync(ClusterConfig cluster, CancellationToken cancel)
    {
        var request = cluster.HttpRequest ?? ForwarderRequestConfig.Empty;

        return ValueTask.FromResult(cluster with { HttpRequest = request with { ActivityTimeout = options.Value.Timeout } });
    }

    /// <summary>Leaves the route as it is.</summary>
    public ValueTask<RouteConfig> ConfigureRouteAsync(RouteConfig route, ClusterConfig? cluster, CancellationToken cancel)
    {
        return ValueTask.FromResult(route);
    }
}
