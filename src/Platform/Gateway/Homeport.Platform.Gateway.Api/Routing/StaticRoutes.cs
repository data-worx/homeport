// SPDX-License-Identifier: AGPL-3.0-or-later

using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>
/// The routes the gateway serves until modules register: Echo only, under the
/// URL scheme of ADR-0013. Replaced by routes from the registry with #42.
/// </summary>
internal static class StaticRoutes
{
    /// <summary>The prefix every module is reached under.</summary>
    internal const string ModulesPrefix = "/modules";

    private const string EchoModuleId = "echo";

    // YARP's name for a route that skips authorization.
    private const string AnonymousPolicy = "anonymous";

    /// <summary>One route per module, stripping <c>/modules/{moduleId}</c>.</summary>
    internal static IReadOnlyList<RouteConfig> Routes { get; } =
    [
        ModuleRoute(EchoModuleId),
    ];

    /// <summary>
    /// One cluster per module. The address is the module id as a service name,
    /// which service discovery resolves from the references the AppHost and the
    /// compose file set.
    /// </summary>
    internal static IReadOnlyList<ClusterConfig> Clusters { get; } =
    [
        ModuleCluster(EchoModuleId),
    ];

    private static RouteConfig ModuleRoute(string moduleId)
    {
        var prefix = $"{ModulesPrefix}/{moduleId}";

        return new RouteConfig
        {
            RouteId = moduleId,
            ClusterId = moduleId,
            Match = new RouteMatch { Path = $"{prefix}/{{**path}}" },

            // Anonymous until the gateway authenticates clients, which comes
            // with #46 ("authenticate clients at the gateway").
            AuthorizationPolicy = AnonymousPolicy,
        }
        .WithTransformPathRemovePrefix(prefix);
    }

    private static ClusterConfig ModuleCluster(string moduleId)
    {
        return new ClusterConfig
        {
            ClusterId = moduleId,
            Destinations = new Dictionary<string, DestinationConfig>
            {
                [moduleId] = new() { Address = $"{Uri.UriSchemeHttp}://{moduleId}" },
            },
        };
    }
}
