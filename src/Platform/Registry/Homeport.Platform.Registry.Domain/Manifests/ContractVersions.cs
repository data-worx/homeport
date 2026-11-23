// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Registry.Domain.Manifests;

/// <summary>The versions of the module contract the registry accepts (ADR-0013).</summary>
public static class ContractVersions
{
    /// <summary>
    /// The previous version and the current. Version 1 was released with
    /// v0.1; version 2 lets a module report a quantity of <c>0</c>, which a
    /// module of version 1 never does, so the registry accepts both.
    /// </summary>
    public static IReadOnlyList<int> Supported { get; } = [1, 2];
}
