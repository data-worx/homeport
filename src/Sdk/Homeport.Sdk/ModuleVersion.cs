// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;

namespace Homeport.Sdk;

/// <summary>Reads a module's own version from its assembly.</summary>
public static class ModuleVersion
{
    /// <summary>
    /// The informational version of <paramref name="assembly"/>, which is
    /// SemVer: the build's <c>Version</c>, optionally followed by
    /// <c>+&lt;commit&gt;</c> as build metadata.
    /// </summary>
    /// <exception cref="InvalidOperationException">The assembly carries no informational version.</exception>
    public static string Of(Assembly assembly)
    {
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? throw new InvalidOperationException($"{assembly.GetName().Name} carries no informational version.");
    }
}
