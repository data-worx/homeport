// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Homeport.Shared.Authentication;

/// <summary>Lets a service accept the tokens the gateway issues for it, and nothing else.</summary>
public static class GatewayTokenAuthenticationExtensions
{
    /// <summary>
    /// How soon after a token with an unknown key arrives the JWKS may be
    /// fetched again. Bounds how long a validator rejects the gateway's new
    /// key after a rotation.
    /// </summary>
    internal static readonly TimeSpan RefreshInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How often the JWKS is fetched regardless. Bounds how long a key removed
    /// from the JWKS is still accepted; five minutes is the library's minimum.
    /// </summary>
    internal static readonly TimeSpan AutomaticRefreshInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Adds JWT bearer authentication as the default scheme, accepting only
    /// tokens the gateway signed for <paramref name="audience"/>. The keys come
    /// from <see cref="GatewayTokenOptions.JwksUri"/>.
    /// </summary>
    /// <param name="services">The services of the validating component.</param>
    /// <param name="audience">The component's own audience, such as <c>homeport-registry</c>.</param>
    public static AuthenticationBuilder AddGatewayTokenAuthentication(this IServiceCollection services, string audience)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        services.AddOptions<GatewayTokenOptions>().BindConfiguration(GatewayTokenOptions.SectionName);
        services.AddHttpClient(JsonWebKeySetRetriever.ClientName);
        services.AddSingleton<GatewayTokenEvents>();

        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IHttpClientFactory, IOptions<GatewayTokenOptions>, ILogger<JsonWebKeySetRetriever>>((bearer, clients, gateway, logger) =>
            {
                bearer.TokenValidationParameters = GatewayTokens.ValidationParameters(audience);
                bearer.MapInboundClaims = false;
                bearer.RefreshOnIssuerKeyNotFound = true;
                bearer.EventsType = typeof(GatewayTokenEvents);

                // Plain HTTP on the internal network: Homeport does not encrypt
                // traffic between components until mTLS is decided (ADR-0014).
                bearer.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    gateway.Value.JwksUri.ToString(),
                    new JsonWebKeySetRetriever(clients, logger))
                {
                    RefreshInterval = RefreshInterval,
                    AutomaticRefreshInterval = AutomaticRefreshInterval,
                };
            });

        return services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
    }
}
