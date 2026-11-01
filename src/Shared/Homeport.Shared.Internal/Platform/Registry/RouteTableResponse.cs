// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.Registry;

/// <summary>What the gateway routes: every module with at least one live instance.</summary>
/// <param name="Modules">Ordered by module id.</param>
public sealed record RouteTableResponse(IReadOnlyList<ModuleRoutesResponse> Modules);
