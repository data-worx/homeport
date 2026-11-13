// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>Where the portal reaches the admin API (ADR-0020).</summary>
public sealed class AdminApiOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "AdminApi";

    /// <summary>
    /// The gateway's admin endpoint, such as <c>http://gateway:8000</c>. It is
    /// a second port of the gateway, which the compose file does not publish.
    /// </summary>
    public Uri? Url { get; set; }
}
