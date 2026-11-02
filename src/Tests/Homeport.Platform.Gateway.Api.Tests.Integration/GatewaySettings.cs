// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>The configuration the gateway does not start, or route, without.</summary>
internal static class GatewaySettings
{
    internal const string KeyId = "current";

    internal const string AdditionalKeyId = "previous";

    /// <summary>
    /// An authority nothing listens on, for a gateway that never sees a client
    /// token: the metadata is fetched with the first token only.
    /// </summary>
    internal const string UnusedAuthority = "http://127.0.0.1:1/";

    /// <summary>A fresh P-256 signing key under <see cref="KeyId"/>.</summary>
    internal static IWebHostBuilder WithSigningKey(this IWebHostBuilder host)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        return host
            .UseSetting("Signing:Key", key.ExportPkcs8PrivateKeyPem())
            .UseSetting("Signing:KeyId", KeyId);
    }

    /// <summary>Reads the routes from <paramref name="registry"/>.</summary>
    internal static IWebHostBuilder WithRegistry(this IWebHostBuilder host, StandInRegistry registry)
    {
        return host.UseSetting("Registry:Url", registry.Url);
    }

    /// <summary>Accepts client tokens of <paramref name="authority"/> for <see cref="StandInIdentityProvider.Audience"/>.</summary>
    internal static IWebHostBuilder WithIdentityProvider(this IWebHostBuilder host, string authority)
    {
        return host
            .UseSetting("ClientAuthentication:Authority", authority)
            .UseSetting("ClientAuthentication:Audience", StandInIdentityProvider.Audience)
            .UseSetting("ClientAuthentication:RequireHttpsMetadata", "false");
    }

    /// <summary>
    /// Accepts client tokens of <paramref name="identityProvider"/> and looks
    /// their clients up at <paramref name="controlPlane"/>.
    /// </summary>
    internal static IWebHostBuilder WithClients(this IWebHostBuilder host, StandInIdentityProvider identityProvider, StandInControlPlane controlPlane)
    {
        return host
            .WithIdentityProvider(identityProvider.Issuer)
            .UseSetting("ControlPlane:Url", controlPlane.Url);
    }

    /// <summary>Sends every request of <paramref name="client"/> with <paramref name="token"/>.</summary>
    internal static HttpClient WithToken(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client;
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
