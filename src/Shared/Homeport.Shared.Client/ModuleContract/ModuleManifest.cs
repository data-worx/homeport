// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.ModuleContract;

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
public sealed record ModuleManifest(
    string Id,
    string Name,
    string Version,
    IReadOnlyList<ManifestRoute> Routes,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> Meters)
{
    /// <summary>Where the manifest is served.</summary>
    public const string ManifestPath = "/.well-known/homeport/manifest";

    /// <summary>Where a module serves its OpenAPI document.</summary>
    public const string OpenApiPath = "/openapi/v1.json";

    /// <summary>The version of the module contract these types describe.</summary>
    public const int CurrentContractVersion = 0;

    /// <summary>The version of the module contract the module implements.</summary>
    public int ContractVersion => CurrentContractVersion;

    /// <summary>Where the module's OpenAPI document is served.</summary>
    public string OpenApiUrl => OpenApiPath;
}
