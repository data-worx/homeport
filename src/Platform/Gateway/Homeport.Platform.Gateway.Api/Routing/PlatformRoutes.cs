// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Authentication;
using Homeport.Platform.Gateway.Domain.ApiDocs;
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
public sealed class PlatformRoutes(IOptions<PlatformRoutesOptions> options)
{
    /// <summary>The prefix every platform endpoint is reached under.</summary>
    internal const string PlatformPrefix = "/platform";

    /// <summary>The route metadata that names the platform service a caller token is issued for.</summary>
    internal const string AudienceMetadata = "Audience";

    /// <summary>The routes and clusters of the platform endpoints, one cluster per service.</summary>
    internal (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build()
    {
        var endpoints = Endpoints();
        var clusters = endpoints
            .Select(endpoint => endpoint.Service)
            .DistinctBy(service => service.Name)
            .ToDictionary(service => service.Name, service => Cluster(service.Name, service.Address), StringComparer.Ordinal);
        var routes = endpoints
            .Select(endpoint => Route(endpoint.Method, endpoint.Path, endpoint.Scope, endpoint.Service.Audience, clusters[endpoint.Service.Name].ClusterId))
            .ToList();

        return (routes, clusters.Values.ToList());
    }

    /// <summary>
    /// The platform endpoints for the platform's API docs, each with the
    /// document of the service behind it and that service's audience.
    /// </summary>
    /// <param name="version">The version of Homeport, which every platform service runs.</param>
    internal IReadOnlyList<(DocumentedRoute Route, string Audience)> Documented(string version)
    {
        return Endpoints()
            .Select(endpoint => (
                new DocumentedRoute(endpoint.Method, endpoint.Path, version, new Uri(endpoint.Service.Address, endpoint.Service.OpenApiPath)),
                endpoint.Service.Audience))
            .ToList();
    }

    // Every platform endpoint, in one place for routing and docs alike.
    private List<PlatformEndpoint> Endpoints()
    {
        var metering = new PlatformService(MeteringService.ServiceName, MeteringService.Audience, options.Value.MeteringUrl, MeteringService.OpenApiPath);

        return [new PlatformEndpoint(HttpMethods.Get, UsageApi.Path, UsageApi.ReadScope, metering)];
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

    private sealed record PlatformService(string Name, string Audience, Uri Address, string OpenApiPath);

    private sealed record PlatformEndpoint(string Method, string Path, string Scope, PlatformService Service);
}
