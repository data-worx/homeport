// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Authentication;
using Homeport.Shared.Internal.Platform.Registry;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>
/// Turns the registry's route table into the gateway's proxy configuration,
/// under the URL scheme of ADR-0013: one route per declared route of a
/// module, going only to the instances that declare it.
/// </summary>
internal static class ModuleRoutes
{
    /// <summary>The prefix every module is reached under.</summary>
    internal const string ModulesPrefix = "/modules";

    /// <summary>The route metadata that names the module a route goes to.</summary>
    internal const string ModuleIdMetadata = "ModuleId";

    /// <summary>The routes and clusters for <paramref name="table"/>, one cluster per route.</summary>
    internal static (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build(RouteTableResponse table)
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var module in table.Modules)
        {
            foreach (var route in module.Routes)
            {
                var id = $"{module.ModuleId} {route.Method} {route.Path}";
                routes.Add(Route(id, module.ModuleId, route));
                clusters.Add(Cluster(id, route.Instances));
            }
        }

        return (routes, clusters);
    }

    private static RouteConfig Route(string id, string moduleId, RouteResponse route)
    {
        var prefix = $"{ModulesPrefix}/{moduleId}";

        return new RouteConfig
        {
            RouteId = id,
            ClusterId = id,
            Match = new RouteMatch { Path = prefix + route.Path, Methods = [route.Method] },
            AuthorizationPolicy = ClientAuthenticationServiceCollectionExtensions.ClientPolicy,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [ModuleIdMetadata] = moduleId,
            },
        }
        .WithTransformPathRemovePrefix(prefix);
    }

    private static ClusterConfig Cluster(string id, IReadOnlyList<Uri> instances)
    {
        return new ClusterConfig
        {
            ClusterId = id,
            Destinations = instances.ToDictionary(
                instance => instance.ToString(),
                instance => new DestinationConfig { Address = instance.ToString() },
                StringComparer.Ordinal),
        };
    }
}
