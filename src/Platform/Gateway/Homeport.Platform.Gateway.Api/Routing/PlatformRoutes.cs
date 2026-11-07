// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Authentication;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>
/// The platform endpoints the gateway forwards to a platform service, under
/// <c>/platform</c> (ADR-0013). They are configured here, never registered
/// (ADR-0019), and stand next to the module routes in every configuration.
/// A platform route is authorised and throttled like a module route, but it
/// is not metered: it carries no <see cref="ModuleRoutes.ModuleIdMetadata"/>.
/// </summary>
internal sealed class PlatformRoutes(IOptions<PlatformRoutesOptions> options)
{
    /// <summary>The prefix every platform endpoint is reached under.</summary>
    internal const string PlatformPrefix = "/platform";

    /// <summary>The route metadata that names the platform service a caller token is issued for.</summary>
    internal const string AudienceMetadata = "Audience";

    /// <summary>The routes and clusters of the platform endpoints, one cluster per service.</summary>
    internal (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build()
    {
        var metering = Cluster(MeteringService.ServiceName, options.Value.MeteringUrl);
        var usage = Route(HttpMethods.Get, UsageApi.Path, UsageApi.ReadScope, MeteringService.Audience, metering.ClusterId);

        return ([usage], [metering]);
    }

    private static RouteConfig Route(string method, string path, string scope, string audience, string clusterId)
    {
        return new RouteConfig
        {
            RouteId = $"{PlatformPrefix} {method} {path}",
            ClusterId = clusterId,
            Match = new RouteMatch { Path = PlatformPrefix + path, Methods = [method] },
            AuthorizationPolicy = ClientAuthenticationServiceCollectionExtensions.ClientPolicy,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ModuleRoutes.ScopeMetadata] = scope,
                [AudienceMetadata] = audience,
            },
        }
        .WithTransformPathRemovePrefix(PlatformPrefix);
    }

    private static ClusterConfig Cluster(string service, Uri address)
    {
        var id = $"{PlatformPrefix} {service}";

        return new ClusterConfig
        {
            ClusterId = id,
            Destinations = new Dictionary<string, DestinationConfig>(StringComparer.Ordinal)
            {
                [service] = new DestinationConfig { Address = address.ToString() },
            },
        };
    }
}
