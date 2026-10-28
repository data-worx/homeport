// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>
/// The gateway's signing key, and an optional additional public key that is
/// published without being signed with. The additional key is what makes a
/// rotation work without downtime: the next key before the switch, the
/// previous one after it.
/// </summary>
public sealed class SigningOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Signing";

    /// <summary>The private key the gateway signs with: P-256, PEM, PKCS#8 or SEC 1.</summary>
    public string? Key { get; set; }

    /// <summary>The <c>kid</c> of <see cref="Key"/>.</summary>
    public string? KeyId { get; set; }

    /// <summary>A public key published next to <see cref="Key"/>: P-256, PEM, SubjectPublicKeyInfo.</summary>
    public string? AdditionalPublicKey { get; set; }

    /// <summary>The <c>kid</c> of <see cref="AdditionalPublicKey"/>.</summary>
    public string? AdditionalKeyId { get; set; }
}
