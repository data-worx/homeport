// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Registry.Domain.Manifests;

/// <summary>What a module instance declares about itself when it registers.</summary>
/// <param name="Id">The module id.</param>
/// <param name="Name">Display name.</param>
/// <param name="Version">The module's own version, SemVer.</param>
/// <param name="ContractVersion">The version of the module contract the module implements.</param>
/// <param name="Routes">Every route the instance serves.</param>
/// <param name="Scopes">The scopes the module defines.</param>
/// <param name="Meters">The meters the module counts in.</param>
/// <param name="OpenApiUrl">Where the instance serves its OpenAPI document, a path below its address.</param>
public sealed record Manifest(
    string Id,
    string Name,
    string Version,
    int ContractVersion,
    IReadOnlyList<ManifestRoute> Routes,
    IReadOnlyList<string> Scopes,
    IReadOnlyList<string> Meters,
    string OpenApiUrl);
