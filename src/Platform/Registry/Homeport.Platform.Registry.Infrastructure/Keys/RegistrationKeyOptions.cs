// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Registry.Infrastructure.Keys;

/// <summary>
/// The module ids the registry accepts and the registration keys valid for
/// each (ADR-0012), such as <c>Registry:RegistrationKeys:echo:0</c>.
/// </summary>
public sealed class RegistrationKeyOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Registry";

    /// <summary>Up to two keys per module id; a second one only during a rotation.</summary>
    public Dictionary<string, string[]> RegistrationKeys { get; init; } = new(StringComparer.Ordinal);
}
