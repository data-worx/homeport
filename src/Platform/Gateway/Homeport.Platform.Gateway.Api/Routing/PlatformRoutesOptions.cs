// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Internal.Platform.ControlPlane;
using Homeport.Shared.Internal.Platform.Metering;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>Where the gateway forwards the platform and admin routes to (ADR-0019, ADR-0020).</summary>
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

    /// <summary>
    /// The control plane, which serves <c>/platform/v1/admin/tenants</c> and
    /// <c>/platform/v1/admin/plans</c>. The default names it by its service
    /// name, which the compose network resolves; the AppHost sets the address
    /// of its endpoint.
    /// </summary>
    public Uri ControlPlaneUrl { get; set; } = new($"{Uri.UriSchemeHttp}://{ControlPlaneService.ServiceName}");
}
