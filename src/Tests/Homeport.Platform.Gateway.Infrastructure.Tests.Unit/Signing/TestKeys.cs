// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using Homeport.Platform.Gateway.Infrastructure.Signing;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.Signing;

/// <summary>Keys in the PEM forms <see cref="SigningOptions"/> takes, made fresh per call.</summary>
internal static class TestKeys
{
    internal static string PrivateP256()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return key.ExportPkcs8PrivateKeyPem();
    }

    internal static string PublicP256()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return key.ExportSubjectPublicKeyInfoPem();
    }

    internal static string PrivateP384()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        return key.ExportPkcs8PrivateKeyPem();
    }

    /// <summary>Options with a fresh signing key under <paramref name="keyId"/> and nothing else.</summary>
    internal static SigningOptions Options(string keyId = "current")
    {
        return new SigningOptions { Key = PrivateP256(), KeyId = keyId };
    }

    internal static SigningKeys Keys(SigningOptions? options = null)
    {
        return new SigningKeys(Microsoft.Extensions.Options.Options.Create(options ?? Options()));
    }
}
