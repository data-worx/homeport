// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Internal.Platform.Metering;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>Where the gateway forwards the platform routes to (ADR-0019).</summary>
public sealed class PlatformRoutesOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "PlatformRoutes";

    /// <summary>
    /// Metering, which serves <c>/platform/v1/usage</c>. The default names it
    /// by its service name, which the compose network resolves; the AppHost
    /// sets the address of its endpoint.
    /// </summary>
    public Uri MeteringUrl { get; set; } = new($"{Uri.UriSchemeHttp}://{MeteringService.ServiceName}");
}
