// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.ServiceDefaults.Proxies;

/// <summary>
/// The reverse proxies in front of a service whose <c>X-Forwarded-For</c>
/// and <c>X-Forwarded-Proto</c> it applies. Empty by default: the service
/// then ignores forwarded headers from every caller, loopback included.
/// </summary>
public sealed class TrustedProxiesOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "TrustedProxies";

    /// <summary>The addresses of trusted proxies, such as <c>10.0.0.5</c>.</summary>
    public IList<string> Proxies { get; } = [];

    /// <summary>The networks of trusted proxies in CIDR notation, such as <c>10.0.0.0/24</c>.</summary>
    public IList<string> Networks { get; } = [];
}
