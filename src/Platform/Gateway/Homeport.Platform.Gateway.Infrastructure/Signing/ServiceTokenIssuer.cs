// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Security.Claims;
using Homeport.Shared.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>
/// Issues the gateway's service tokens for its calls to platform services
/// (ADR-0014): audience the called component, subject the gateway. A token
/// is reused per audience until it is due for renewal.
/// </summary>
public sealed class ServiceTokenIssuer(SigningKeys keys, TimeProvider time)
{
    /// <summary>How long before its expiry a token is replaced, so none expires in flight.</summary>
    internal static readonly TimeSpan RenewBefore = TimeSpan.FromMinutes(1);

    private readonly JsonWebTokenHandler handler = new();
    private readonly ConcurrentDictionary<string, IssuedToken> issued = new(StringComparer.Ordinal);

    /// <summary>A valid service token for <paramref name="audience"/>.</summary>
    /// <param name="audience">The called component, such as <c>homeport-registry</c>.</param>
    public string Issue(string audience)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        var now = time.GetUtcNow();
        if (issued.TryGetValue(audience, out var token) && now < token.ExpiresAt - RenewBefore)
        {
            return token.Value;
        }

        var renewed = Create(audience, now);
        issued[audience] = renewed;

        return renewed.Value;
    }

    private IssuedToken Create(string audience, DateTimeOffset now)
    {
        var expiresAt = now + GatewayTokens.MaximumLifetime;
        var value = handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = GatewayTokens.Issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, GatewayTokens.GatewaySubject)]),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = keys.Credentials,
        });

        return new IssuedToken(value, expiresAt);
    }

    private sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);
}
