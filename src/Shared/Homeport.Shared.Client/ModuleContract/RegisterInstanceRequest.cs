// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.ModuleContract;

/// <summary>
/// A module instance registering at the registry, at
/// <see cref="RegistryPaths.Instance"/>, with its registration key as bearer token.
/// </summary>
/// <param name="Address">Where the gateway reaches the instance, such as <c>http://echo-1:8080</c>.</param>
/// <param name="Manifest">What the instance declares about itself.</param>
public sealed record RegisterInstanceRequest(Uri Address, ModuleManifest Manifest);
