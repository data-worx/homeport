// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Registry.V1;

/// <summary>A route the gateway serves for a module.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The path below the module.</param>
/// <param name="Scope">The scope a caller needs.</param>
/// <param name="Meter">The meter a request is counted in.</param>
/// <param name="Quantity">How much one request counts.</param>
/// <param name="Version">
/// The version of the instance whose declaration applies, the highest among
/// the instances that declare the route.
/// </param>
public sealed record ModuleRouteResponse(
    string Method,
    string Path,
    string Scope,
    string Meter,
    int Quantity,
    string Version);
