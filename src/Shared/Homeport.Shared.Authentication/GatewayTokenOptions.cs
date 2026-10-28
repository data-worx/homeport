// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Authentication;

/// <summary>Where a validating service finds the gateway's keys.</summary>
public sealed class GatewayTokenOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "GatewayTokens";

    /// <summary>The gateway's service name, as the AppHost declares it.</summary>
    public const string GatewayServiceName = "gateway";

    /// <summary>
    /// The gateway's JWKS. The default names the gateway by its service name,
    /// which service discovery resolves from the reference the AppHost and the
    /// compose file set.
    /// </summary>
    public Uri JwksUri { get; set; } = new($"{Uri.UriSchemeHttp}://{GatewayServiceName}{GatewayTokens.JwksPath}");
}
