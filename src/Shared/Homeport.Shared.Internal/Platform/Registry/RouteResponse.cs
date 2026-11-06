// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.Registry;

/// <summary>One route of a module and the instances that serve it.</summary>
/// <param name="Method">The HTTP method.</param>
/// <param name="Path">The path below the module.</param>
/// <param name="Scope">The scope a caller needs.</param>
/// <param name="Meter">The meter a request is counted in.</param>
/// <param name="Quantity">How much one request counts.</param>
/// <param name="Instances">The addresses of the instances that declare the route.</param>
/// <param name="Version">The version of the instance whose attributes apply.</param>
/// <param name="OpenApiUrl">The OpenAPI document of that instance, which documents the route (ADR-0017).</param>
public sealed record RouteResponse(
    string Method,
    string Path,
    string Scope,
    string Meter,
    int Quantity,
    IReadOnlyList<Uri> Instances,
    string Version,
    Uri OpenApiUrl);
