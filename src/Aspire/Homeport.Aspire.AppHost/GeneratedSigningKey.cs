// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Homeport.Aspire.AppHost;

/// <summary>
/// Generates the gateway's signing key for a local run: a fresh P-256 private
/// key in PKCS#8 PEM, kept in the user secrets like the generated passwords.
/// </summary>
internal sealed class GeneratedSigningKey : ParameterDefault
{
    /// <summary>Generates a new P-256 signing key as PKCS#8 PEM.</summary>
    public override string GetDefaultValue()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return key.ExportPkcs8PrivateKeyPem();
    }

    /// <summary>
    /// Writes no default: a published deployment never gets a generated key,
    /// the operator supplies their own.
    /// </summary>
    public override void WriteToManifest(ManifestPublishingContext context)
    {
    }
}
