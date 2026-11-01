// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.Registry;

/// <summary>The routes of one module: the union of what its live instances declare.</summary>
/// <param name="ModuleId">The module id.</param>
/// <param name="Routes">Ordered by path, then method.</param>
public sealed record ModuleRoutesResponse(string ModuleId, IReadOnlyList<RouteResponse> Routes);
