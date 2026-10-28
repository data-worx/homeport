// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.IdentityModel.Tokens;

namespace Homeport.Shared.Authentication;

/// <summary>
/// The tokens the gateway issues (ADR-0011, ADR-0014): who issues them, how
/// they are signed, where the keys are published and how they are validated.
/// The gateway and every service that validates its tokens read them from
/// here, so both sides cannot drift apart.
/// </summary>
public static class GatewayTokens
{
    /// <summary>The issuer of every token the gateway signs.</summary>
    public const string Issuer = "homeport-gateway";

    /// <summary>The subject of a service token: the gateway calling as itself.</summary>
    public const string GatewaySubject = "homeport-gateway";

    /// <summary>ES256, the one algorithm the gateway signs with and a validator accepts.</summary>
    public const string SigningAlgorithm = SecurityAlgorithms.EcdsaSha256;

    /// <summary>Where the gateway publishes its public keys.</summary>
    public const string JwksPath = "/.well-known/jwks.json";

    /// <summary>The longest a service token may live (ADR-0014).</summary>
    public static readonly TimeSpan MaximumLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// The clock difference tolerated between the gateway and a validator.
    /// Smaller than the library default of five minutes, which would double
    /// the lifetime of a service token.
    /// </summary>
    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    /// <summary>
    /// What a token must satisfy to be accepted for <paramref name="audience"/>:
    /// issued by the gateway, for that audience, signed with ES256, not expired.
    /// </summary>
    /// <param name="audience">The component that validates, such as <c>homeport-registry</c>.</param>
    /// <param name="signingKeys">
    /// The published keys. Left out where a configuration manager supplies
    /// them from the JWKS, as <see cref="GatewayTokenAuthenticationExtensions"/> does.
    /// </param>
    public static TokenValidationParameters ValidationParameters(
        string audience,
        IEnumerable<SecurityKey>? signingKeys = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        return new TokenValidationParameters
        {
            ValidIssuer = Issuer,
            ValidAudience = audience,
            ValidAlgorithms = [SigningAlgorithm],
            IssuerSigningKeys = signingKeys,
            RequireSignedTokens = true,
            RequireExpirationTime = true,
            ClockSkew = ClockSkew,
        };
    }
}
