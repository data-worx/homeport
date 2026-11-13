// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using Homeport.Platform.Gateway.Api.Admin;
using Homeport.Platform.Gateway.Application.Throttling;
using Homeport.Platform.Gateway.Infrastructure.Throttling;
using Homeport.Platform.Gateway.Infrastructure.Usage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

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

    // A broker nothing listens on: the gateway's transports are stubbed, and
    // what it publishes is tracked rather than sent.
    private const string UnusedBroker = "amqp://guest:guest@127.0.0.1:1/";

    // A Valkey nothing listens on, given up on quickly: the gateway connects
    // while it starts, and waits that long before it starts without Valkey.
    private const string UnusedValkey = "127.0.0.1:1,connectTimeout=100";

    /// <summary>Publishes usage events to Wolverine's stub transports instead of a broker.</summary>
    internal static IWebHostBuilder WithUsage(this IWebHostBuilder host)
    {
        return host
            .UseSetting($"ConnectionStrings:{UsageServiceCollectionExtensions.ConnectionName}", UnusedBroker)
            .ConfigureTestServices(services => services.DisableAllExternalWolverineTransports());
    }

    /// <summary>
    /// Draws the rate limit from <paramref name="buckets"/>, or from buckets
    /// that are always full, rather than from Valkey.
    /// </summary>
    internal static IWebHostBuilder WithTokenBuckets(this IWebHostBuilder host, ITokenBuckets? buckets = null)
    {
        return host
            .WithUnreachableValkey()
            .ConfigureTestServices(services => services.AddSingleton(buckets ?? new StandInTokenBuckets()));
    }

    /// <summary>Draws the rate limit from a Valkey nothing listens on, so every request passes unthrottled.</summary>
    internal static IWebHostBuilder WithUnreachableValkey(this IWebHostBuilder host)
    {
        return host.UseSetting($"ConnectionStrings:{ThrottlingServiceCollectionExtensions.ValkeyConnectionName}", UnusedValkey);
    }

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

    /// <summary>
    /// Serves the admin API on <paramref name="port"/> for operator tokens of
    /// <paramref name="identityProvider"/>, forwarding <paramref name="endpoints"/>
    /// in place of the admin endpoints of the release.
    /// </summary>
    internal static IWebHostBuilder WithAdminApi(this IWebHostBuilder host, int port, StandInIdentityProvider identityProvider, params AdminEndpoint[] endpoints)
    {
        return host
            .UseSetting("AdminApi:Port", port.ToString(CultureInfo.InvariantCulture))
            .UseSetting("AdminApi:Authority", identityProvider.Issuer)
            .UseSetting("AdminApi:Audience", StandInIdentityProvider.OperatorAudience)
            .UseSetting("AdminApi:RequireHttpsMetadata", "false")
            .ConfigureTestServices(services => services.AddSingleton(new AdminRoutes(endpoints)));
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
