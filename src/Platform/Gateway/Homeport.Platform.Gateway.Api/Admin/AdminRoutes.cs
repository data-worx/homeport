// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Homeport.Platform.Gateway.Api.Admin;

/// <summary>
/// The admin endpoints the gateway forwards to a platform service, under
/// <see cref="AdminPrefix"/> (ADR-0020). Like the platform routes they are
/// configured in the gateway, never registered (ADR-0019). An admin route
/// requires the operator's token with the admin claim, is neither throttled
/// nor metered, and reaches the service with a caller token of scope
/// <c>admin</c>.
/// </summary>
/// <param name="endpoints">Every admin endpoint the gateway forwards.</param>
public sealed class AdminRoutes(IReadOnlyList<AdminEndpoint> endpoints)
{
    /// <summary>The prefix every admin endpoint is reached under, on the admin port only.</summary>
    internal const string AdminPrefix = PlatformRoutes.PlatformPrefix + "/v1/admin";

    /// <summary>
    /// The route metadata that marks an admin route and names the platform
    /// service the operator's caller token is issued for.
    /// </summary>
    internal const string AdminAudienceMetadata = "AdminAudience";

    /// <summary>
    /// The admin endpoints of a release. Each one a platform service serves is
    /// added here, with the service's address from <paramref name="options"/>.
    /// </summary>
    internal static AdminRoutes Configured(PlatformRoutesOptions options)
    {
        var controlPlane = new AdminService(ControlPlaneService.ServiceName, ControlPlaneService.Audience, options.ControlPlaneUrl);

        return new AdminRoutes(
        [
            new AdminEndpoint(HttpMethods.Get, TenantsApi.Path, controlPlane),
            new AdminEndpoint(HttpMethods.Post, TenantsApi.Path, controlPlane),
            new AdminEndpoint(HttpMethods.Get, TenantsApi.TenantPath, controlPlane),
            new AdminEndpoint(HttpMethods.Put, TenantsApi.TenantPath, controlPlane),
        ]);
    }

    /// <summary>The routes and clusters of the admin endpoints, one cluster per service.</summary>
    internal (IReadOnlyList<RouteConfig> Routes, IReadOnlyList<ClusterConfig> Clusters) Build()
    {
        // Anywhere else, the admin port would never reach it, and the public
        // endpoint would.
        if (endpoints.FirstOrDefault(endpoint => !(PlatformRoutes.PlatformPrefix + endpoint.Path).StartsWith(AdminPrefix + "/", StringComparison.Ordinal)) is { } outside)
        {
            throw new InvalidOperationException($"The admin endpoint {outside.Method} {outside.Path} does not lie below {AdminPrefix}.");
        }

        var clusters = endpoints
            .Select(endpoint => endpoint.Service)
            .DistinctBy(service => service.Name)
            .ToDictionary(service => service.Name, service => Cluster(service.Name, service.Address), StringComparer.Ordinal);
        var routes = endpoints
            .Select(endpoint => Route(endpoint, clusters[endpoint.Service.Name].ClusterId))
            .ToList();

        return (routes, clusters.Values.ToList());
    }

    private static RouteConfig Route(AdminEndpoint endpoint, string clusterId)
    {
        return new RouteConfig
        {
            RouteId = $"{AdminPrefix} {endpoint.Method} {endpoint.Path}",
            ClusterId = clusterId,
            Match = new RouteMatch { Path = PlatformRoutes.PlatformPrefix + endpoint.Path, Methods = [endpoint.Method] },
            AuthorizationPolicy = AdminApiServiceCollectionExtensions.AdminPolicy,
            Metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [AdminAudienceMetadata] = endpoint.Service.Audience,
            },
        }
        .WithTransformPathRemovePrefix(PlatformRoutes.PlatformPrefix);
    }

    // Its own cluster id, so it never collides with the platform route's
    // cluster of the same service, whose address may differ.
    private static ClusterConfig Cluster(string service, Uri address)
    {
        var id = $"{AdminPrefix} {service}";

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

/// <summary>A platform service that serves admin endpoints.</summary>
/// <param name="Name">The service's name, which names its cluster.</param>
/// <param name="Audience">The audience the service validates caller tokens for.</param>
/// <param name="Address">Where the gateway reaches the service.</param>
public sealed record AdminService(string Name, string Audience, Uri Address);

/// <summary>An admin endpoint of a platform service.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The path at the service, such as <c>/v1/admin/tenants</c>; the gateway serves it below <c>/platform</c>.</param>
/// <param name="Service">The service that serves it.</param>
public sealed record AdminEndpoint(string Method, string Path, AdminService Service);
