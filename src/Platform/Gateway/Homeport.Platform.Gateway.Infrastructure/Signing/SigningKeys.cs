// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using Homeport.Shared.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>
/// The keys of <see cref="SigningOptions"/>, read once: the credentials the
/// gateway signs with and the JWKS it publishes.
/// </summary>
public sealed class SigningKeys : IDisposable
{
    private readonly ECDsa current;
    private readonly ECDsa? additional;

    /// <summary>Reads the keys; <see cref="SigningOptionsValidator"/> has already checked them.</summary>
    public SigningKeys(IOptions<SigningOptions> options)
    {
        var signing = options.Value;

        if (!SigningKeyPem.TryReadPrivate(signing.Key ?? string.Empty, out var key))
        {
            throw new InvalidOperationException($"{SigningOptions.SectionName}:{nameof(SigningOptions.Key)} is not a P-256 private key in PEM.");
        }

        current = key;
        Credentials = new SigningCredentials(
            new ECDsaSecurityKey(current) { KeyId = signing.KeyId },
            GatewayTokens.SigningAlgorithm);

        List<PublicJsonWebKey> published = [PublicJsonWebKey.FromKey(current, signing.KeyId!)];
        if (!string.IsNullOrWhiteSpace(signing.AdditionalPublicKey)
            && SigningKeyPem.TryReadPublic(signing.AdditionalPublicKey, out additional))
        {
            published.Add(PublicJsonWebKey.FromKey(additional, signing.AdditionalKeyId!));
        }

        Jwks = new JsonWebKeySetDocument(published);
    }

    /// <summary>What every token the gateway issues is signed with.</summary>
    public SigningCredentials Credentials { get; }

    /// <summary>The public keys the gateway publishes, the signing key first.</summary>
    public JsonWebKeySetDocument Jwks { get; }

    public void Dispose()
    {
        current.Dispose();
        additional?.Dispose();
    }
}
