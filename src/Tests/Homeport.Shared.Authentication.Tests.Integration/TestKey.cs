// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Shared.Authentication.Tests.Integration;

/// <summary>
/// A P-256 key standing in for the gateway's: signs tokens the way the gateway
/// does and is published the way the gateway publishes it.
/// </summary>
internal sealed class TestKey(string keyId) : IDisposable
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

    internal string KeyId { get; } = keyId;

    /// <summary>The JWKS publishing <paramref name="keys"/>, as the gateway serves it.</summary>
    internal static string Jwks(params TestKey[] keys)
    {
        return JsonSerializer.Serialize(new JsonWebKeySetResponse(
            keys.Select(published => PublicJsonWebKey.FromKey(published.key, published.KeyId)).ToList()));
    }

    /// <summary>The signing keys a validator reads from <see cref="Jwks"/>.</summary>
    internal static IList<SecurityKey> SigningKeys(params TestKey[] keys)
    {
        return new JsonWebKeySet(Jwks(keys)).GetSigningKeys();
    }

    /// <summary>A service token for <paramref name="audience"/>, expiring <paramref name="expiresIn"/> from now.</summary>
    internal string Sign(string audience, TimeSpan? expiresIn = null, string issuer = GatewayTokens.Issuer)
    {
        var expires = TimeProvider.System.GetUtcNow().UtcDateTime + (expiresIn ?? Lifetime);

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, GatewayTokens.GatewaySubject)]),
            IssuedAt = expires - Lifetime,
            NotBefore = expires - Lifetime,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new ECDsaSecurityKey(key) { KeyId = KeyId },
                GatewayTokens.SigningAlgorithm),
        });
    }

    public void Dispose()
    {
        key.Dispose();
    }
}
