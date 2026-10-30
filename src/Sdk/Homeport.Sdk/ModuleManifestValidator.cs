// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.RegularExpressions;
using Homeport.Shared.Client.ModuleContract;
using Microsoft.AspNetCore.Http;

namespace Homeport.Sdk;

/// <summary>
/// Checks a manifest against the module contract before the module starts,
/// so a broken manifest fails on the developer's machine.
/// </summary>
internal static partial class ModuleManifestValidator
{
    private const string ReservedId = "platform";

    private static readonly string[] Methods =
        [HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete];

    /// <summary>Every way <paramref name="manifest"/> breaks the module contract; empty when it holds.</summary>
    internal static IReadOnlyList<string> Validate(ModuleManifest manifest)
    {
        var problems = new List<string>();

        if (!IdPattern().IsMatch(manifest.Id))
        {
            problems.Add($"Id '{manifest.Id}' must start with a lower-case letter and contain only lower-case letters, digits and '-'.");
        }

        if (manifest.Id == ReservedId)
        {
            problems.Add($"Id '{ReservedId}' is reserved.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            problems.Add("Name must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            problems.Add("Version must not be empty.");
        }

        if (manifest.Routes.Count == 0)
        {
            problems.Add("At least one route must be declared.");
        }

        foreach (var route in manifest.Routes)
        {
            problems.AddRange(ValidateRoute(manifest, route));
        }

        return problems;
    }

    private static IEnumerable<string> ValidateRoute(ModuleManifest manifest, ManifestRoute route)
    {
        var name = $"Route {route.Method} {route.Path}";

        if (!Methods.Contains(route.Method))
        {
            yield return $"{name}: method must be one of {string.Join(", ", Methods)}.";
        }

        if (!PathPattern().IsMatch(route.Path))
        {
            yield return $"{name}: path must start with /v{{major}}/.";
        }

        if (!manifest.Scopes.Contains(route.Scope))
        {
            yield return $"{name}: scope '{route.Scope}' is not declared in the manifest's scopes.";
        }

        if (!manifest.Meters.Contains(route.Meter))
        {
            yield return $"{name}: meter '{route.Meter}' is not declared in the manifest's meters.";
        }

        if (route.Quantity < 1)
        {
            yield return $"{name}: quantity must be at least 1.";
        }
    }

    [GeneratedRegex("^[a-z][a-z0-9-]*$")]
    private static partial Regex IdPattern();

    [GeneratedRegex("^/v[1-9][0-9]*/")]
    private static partial Regex PathPattern();
}
