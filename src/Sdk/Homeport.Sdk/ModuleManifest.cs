// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Homeport.Sdk;

/// <summary>
/// What a module declares about itself at <see cref="ManifestPath"/>, as the
/// module contract defines it.
/// </summary>
/// <param name="Id">Stable module id, lower case; <c>platform</c> is reserved.</param>
/// <param name="Name">Display name.</param>
/// <param name="Version">The module's own version, SemVer.</param>
/// <param name="Routes">Every route the module serves.</param>
/// <param name="Scopes">The scopes the module defines.</param>
/// <param name="Meters">The meters the module counts in.</param>
public sealed partial record ModuleManifest(
    string Id,
    string Name,
    string Version,
    IReadOnlyList<ManifestRoute> Routes,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> Meters)
{
    /// <summary>Where the manifest is served.</summary>
    public const string ManifestPath = "/.well-known/homeport/manifest";

    /// <summary>Where the SDK serves the module's OpenAPI document.</summary>
    public const string OpenApiPath = "/openapi/v1.json";

    /// <summary>The version of the module contract this SDK implements.</summary>
    public const int CurrentContractVersion = 0;

    private const string ReservedId = "platform";

    private static readonly string[] Methods =
        [HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete];

    /// <summary>The version of the module contract the module implements.</summary>
    public int ContractVersion => CurrentContractVersion;

    /// <summary>Where the module's OpenAPI document is served.</summary>
    public string OpenApiUrl => OpenApiPath;

    /// <summary>Every way this manifest breaks the module contract; empty when it holds.</summary>
    public IReadOnlyList<string> Validate()
    {
        var problems = new List<string>();

        if (!IdPattern().IsMatch(Id))
        {
            problems.Add($"Id '{Id}' must start with a lower-case letter and contain only lower-case letters, digits and '-'.");
        }

        if (Id == ReservedId)
        {
            problems.Add($"Id '{ReservedId}' is reserved.");
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            problems.Add("Name must not be empty.");
        }

        if (string.IsNullOrWhiteSpace(Version))
        {
            problems.Add("Version must not be empty.");
        }

        if (Routes.Count == 0)
        {
            problems.Add("At least one route must be declared.");
        }

        foreach (var route in Routes)
        {
            problems.AddRange(ValidateRoute(route));
        }

        return problems;
    }

    private IEnumerable<string> ValidateRoute(ManifestRoute route)
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

        if (!Scopes.Contains(route.Scope))
        {
            yield return $"{name}: scope '{route.Scope}' is not declared in the manifest's scopes.";
        }

        if (!Meters.Contains(route.Meter))
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
