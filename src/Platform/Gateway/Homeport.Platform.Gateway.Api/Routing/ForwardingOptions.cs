// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>How the gateway forwards a request to a module or a platform service.</summary>
public sealed class ForwardingOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Gateway:Forwarding";

    /// <summary>The timeout when nothing else is configured.</summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long a forwarded request may stay idle, with neither a byte sent
    /// nor received, before the gateway gives up and answers <c>504</c>.
    /// Applies to every module, platform and admin route.
    /// </summary>
    public TimeSpan Timeout { get; set; } = DefaultTimeout;
}
