// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Registry.Domain.Manifests;

/// <summary>The versions of the module contract the registry accepts (ADR-0013).</summary>
public static class ContractVersions
{
    /// <summary>
    /// The current version and, once there is one, the previous. Version 1 is
    /// the first released with v0.1; the drafts before it were never released,
    /// so there is no previous version yet.
    /// </summary>
    public static IReadOnlyList<int> Supported { get; } = [1];
}
