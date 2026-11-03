// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Internal.Platform.ControlPlane;

namespace Homeport.Platform.Gateway.Infrastructure.ControlPlane;

/// <summary>Where the gateway looks clients up, and how long it remembers the answer.</summary>
public sealed class ControlPlaneOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "ControlPlane";

    /// <summary>
    /// The control plane. The default names it by its service name, which
    /// service discovery resolves from the reference the AppHost and the
    /// compose file set.
    /// </summary>
    public Uri Url { get; set; } = new($"{Uri.UriSchemeHttp}://{ControlPlaneService.ServiceName}");

    /// <summary>
    /// How long the answer for a client is kept, known or unknown. A client
    /// the operator adds or removes takes effect at the gateway within it.
    /// </summary>
    public TimeSpan ClientCacheLifetime { get; set; } = TimeSpan.FromMinutes(1);
}
