// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;

namespace Homeport.Platform.Registry.Domain.Routing;

/// <summary>A live instance of a module: where it is reached and what it declares.</summary>
/// <param name="InstanceId">Unique per running instance; chosen by the instance.</param>
/// <param name="Address">Where the gateway reaches the instance.</param>
/// <param name="Manifest">The manifest the instance registered with.</param>
public sealed record Instance(string InstanceId, Uri Address, Manifest Manifest);
