// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Domain.ApiDocs;

/// <summary>A route the gateway serves, and the document that describes it.</summary>
/// <param name="Method">The HTTP method, as the manifest declares it.</param>
/// <param name="Path">The path below the module, as the manifest declares it.</param>
/// <param name="Version">The version of the instance whose document describes the route.</param>
/// <param name="OpenApiUrl">That instance's OpenAPI document.</param>
public sealed record DocumentedRoute(string Method, string Path, string Version, Uri OpenApiUrl);
