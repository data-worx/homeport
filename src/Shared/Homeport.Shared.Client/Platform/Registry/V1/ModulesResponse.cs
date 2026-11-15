// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Registry.V1;

/// <summary>Every module with at least one live instance.</summary>
/// <param name="Modules">The modules, ordered by module id.</param>
public sealed record ModulesResponse(IReadOnlyList<ModuleResponse> Modules);
