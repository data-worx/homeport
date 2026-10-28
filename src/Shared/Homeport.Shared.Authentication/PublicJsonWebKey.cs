// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Shared.Authentication;

/// <summary>
/// One public EC key as the gateway publishes it in its JWKS (RFC 7517,
/// RFC 7518). Holds the public point only, so private key material cannot
/// reach the document.
/// </summary>
/// <param name="Kty">Key type, always <c>EC</c>.</param>
/// <param name="Use">Intended use, always <c>sig</c>.</param>
/// <param name="Alg">Algorithm, always ES256.</param>
/// <param name="Kid">The key id a token names in its header.</param>
/// <param name="Crv">Curve, always <c>P-256</c>.</param>
/// <param name="X">X coordinate of the public point, base64url.</param>
/// <param name="Y">Y coordinate of the public point, base64url.</param>
public sealed record PublicJsonWebKey(
    [property: JsonPropertyName("kty")] string Kty,
    [property: JsonPropertyName("use")] string Use,
    [property: JsonPropertyName("alg")] string Alg,
    [property: JsonPropertyName("kid")] string Kid,
    [property: JsonPropertyName("crv")] string Crv,
    [property: JsonPropertyName("x")] string X,
    [property: JsonPropertyName("y")] string Y)
{
    /// <summary>The public part of <paramref name="key"/>, published under <paramref name="keyId"/>.</summary>
    public static PublicJsonWebKey FromKey(ECDsa key, string keyId)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);

        var point = key.ExportParameters(includePrivateParameters: false).Q;

        return new PublicJsonWebKey(
            JsonWebAlgorithmsKeyTypes.EllipticCurve,
            JsonWebKeyUseNames.Sig,
            GatewayTokens.SigningAlgorithm,
            keyId,
            JsonWebKeyECTypes.P256,
            Base64UrlEncoder.Encode(point.X),
            Base64UrlEncoder.Encode(point.Y));
    }
}
