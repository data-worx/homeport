// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;

namespace Homeport.Platform.Registry.Domain.Routing;

/// <summary>
/// Builds what the gateway routes from the live instances (ADR-0013): per
/// module the union of the routes its instances declare, each going only to
/// the instances that declare it.
/// </summary>
public static class RouteTable
{
    /// <summary>
    /// The routes of every module with at least one live instance, ordered by
    /// module id, so the same instances always give the same table.
    /// </summary>
    /// <remarks>
    /// A route is identified by method and path. Where instances declare it
    /// with different attributes, those of the instance with the highest
    /// <see cref="Manifest.Version"/> apply; between equal versions, the
    /// lowest instance id decides, so the choice does not depend on order.
    /// The same instance documents the route (ADR-0017).
    /// </remarks>
    public static IReadOnlyList<ModuleRoutes> Build(IEnumerable<Instance> instances)
    {
        return instances
            .GroupBy(instance => instance.Manifest.Id, StringComparer.Ordinal)
            .OrderBy(module => module.Key, StringComparer.Ordinal)
            .Select(module => new ModuleRoutes(module.Key, Merge(module)))
            .ToList();
    }

    private static List<Route> Merge(IEnumerable<Instance> instances)
    {
        return instances
            .SelectMany(instance => instance.Manifest.Routes.Select(route => (Instance: instance, Route: route)))
            .GroupBy(declared => (declared.Route.Method, declared.Route.Path))
            .OrderBy(route => route.Key.Path, StringComparer.Ordinal)
            .ThenBy(route => route.Key.Method, StringComparer.Ordinal)
            .Select(ToRoute)
            .ToList();
    }

    private static Route ToRoute(IEnumerable<(Instance Instance, ManifestRoute Route)> declarations)
    {
        var declared = declarations.ToList();
        var winner = declared
            .OrderByDescending(declaration => declaration.Instance.Manifest.Version, SemanticVersions.Precedence)
            .ThenBy(declaration => declaration.Instance.InstanceId, StringComparer.Ordinal)
            .First();

        var addresses = declared
            .Select(declaration => declaration.Instance.Address)
            .Distinct()
            .OrderBy(address => address.ToString(), StringComparer.Ordinal)
            .ToList();

        var (instance, route) = winner;

        return new Route(
            route.Method,
            route.Path,
            route.Scope,
            route.Meter,
            route.Quantity,
            addresses,
            instance.Manifest.Version,
            new Uri(instance.Address, instance.Manifest.OpenApiUrl));
    }
}
