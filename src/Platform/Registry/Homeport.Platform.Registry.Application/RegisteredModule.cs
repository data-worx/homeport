// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Routing;

namespace Homeport.Platform.Registry.Application;

/// <summary>A module with at least one live instance, as the operator sees it.</summary>
/// <param name="ModuleId">The module id.</param>
/// <param name="Instances">Its live instances, ordered by instance id.</param>
/// <param name="Routes">The routes the gateway serves for it, as the route table builds them.</param>
public sealed record RegisteredModule(string ModuleId, IReadOnlyList<RegisteredInstance> Instances, IReadOnlyList<Route> Routes);

/// <summary>A live instance and how long its registration lasts.</summary>
/// <param name="Instance">The instance.</param>
/// <param name="LastHeartbeatAt">When it last registered or sent a heartbeat.</param>
/// <param name="ExpiresAt">When its registration ends unless another heartbeat arrives.</param>
public sealed record RegisteredInstance(Instance Instance, DateTimeOffset LastHeartbeatAt, DateTimeOffset ExpiresAt);
