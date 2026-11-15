// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Registry.V1;

/// <summary>A registered module as the operator sees it.</summary>
/// <param name="ModuleId">The module id, which names it below <c>/modules/</c>.</param>
/// <param name="Instances">Its live instances, ordered by instance id.</param>
/// <param name="Routes">The routes the gateway serves for it, ordered by path, then method.</param>
public sealed record ModuleResponse(
    string ModuleId,
    IReadOnlyList<ModuleInstanceResponse> Instances,
    IReadOnlyList<ModuleRouteResponse> Routes);
