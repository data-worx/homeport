// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Sdk.Registration;

/// <summary>
/// Where a module instance registers and how it says who it is, bound from
/// <c>Homeport:Registry</c>. Without <see cref="Url"/>, it does not register.
/// </summary>
internal sealed class RegistrationOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    internal const string SectionName = "Homeport:Registry";

    /// <summary>The registry, such as <c>http://registry</c>.</summary>
    public Uri? Url { get; set; }

    /// <summary>The module's registration key (ADR-0012). A secret.</summary>
    public string? Key { get; set; }

    /// <summary>Where the gateway reaches this instance, such as <c>http://echo-1:8080</c>.</summary>
    public Uri? InstanceAddress { get; set; }
}
