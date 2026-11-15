// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Routing;

namespace Homeport.Platform.Registry.Application;

/// <summary>An instance as the store keeps it.</summary>
/// <param name="Instance">The instance.</param>
/// <param name="TimeToLive">How long the store keeps it without another heartbeat.</param>
public sealed record StoredInstance(Instance Instance, TimeSpan TimeToLive);
