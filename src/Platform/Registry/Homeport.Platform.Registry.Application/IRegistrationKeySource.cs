// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Registry.Application;

/// <summary>The registration keys the operator configured (ADR-0012).</summary>
public interface IRegistrationKeySource
{
    /// <summary>The keys valid for <paramref name="moduleId"/>; empty for a module id the registry does not know.</summary>
    IReadOnlyList<string> KeysFor(string moduleId);
}
