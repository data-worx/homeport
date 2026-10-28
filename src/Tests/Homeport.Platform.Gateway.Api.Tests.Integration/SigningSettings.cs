// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>The signing configuration the gateway does not start without.</summary>
internal static class SigningSettings
{
    internal const string KeyId = "current";

    internal const string AdditionalKeyId = "previous";

    /// <summary>A fresh P-256 signing key under <see cref="KeyId"/>.</summary>
    internal static IWebHostBuilder WithSigningKey(this IWebHostBuilder host)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        return host
            .UseSetting("Signing:Key", key.ExportPkcs8PrivateKeyPem())
            .UseSetting("Signing:KeyId", KeyId);
    }

    /// <summary>A fresh P-256 public key under <see cref="AdditionalKeyId"/>.</summary>
    internal static IWebHostBuilder WithAdditionalPublicKey(this IWebHostBuilder host)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        return host
            .UseSetting("Signing:AdditionalPublicKey", key.ExportSubjectPublicKeyInfoPem())
            .UseSetting("Signing:AdditionalKeyId", AdditionalKeyId);
    }
}
