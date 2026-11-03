// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Infrastructure.ControlPlane;

/// <summary>
/// Looks clients up at the control plane, with a service token for it
/// (ADR-0014), and keeps each answer for
/// <see cref="ControlPlaneOptions.ClientCacheLifetime"/>.
/// </summary>
public sealed class ControlPlaneClient(IHttpClientFactory clients, IMemoryCache cache, IOptions<ControlPlaneOptions> options)
{
    /// <summary>The name of the HTTP client that calls the control plane.</summary>
    internal const string ClientName = "Homeport.ControlPlane";

    private const string CacheKeyPrefix = "control-plane-client:";

    /// <summary>The tenant and plan of <paramref name="clientId"/>.</summary>
    /// <param name="clientId">The client id as the identity provider issued it.</param>
    /// <param name="cancellationToken">Aborts the lookup.</param>
    /// <returns><c>null</c> when the control plane knows no such client.</returns>
    /// <exception cref="ControlPlaneUnavailableException">The control plane could not be reached or answered with an error.</exception>
    public async Task<ClientResponse?> FindAsync(string clientId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);

        // An unknown client is kept too, so a valid token of a client the
        // operator never mapped does not reach the control plane on every request.
        var key = CacheKeyPrefix + clientId;
        if (cache.TryGetValue(key, out ClientResponse? cached))
        {
            return cached;
        }

        var client = await ReadAsync(clientId, cancellationToken);
        cache.Set(key, client, options.Value.ClientCacheLifetime);

        return client;
    }

    private async Task<ClientResponse?> ReadAsync(string clientId, CancellationToken cancellationToken)
    {
        var path = ControlPlaneService.ClientPath.Replace(
            "{" + ControlPlaneService.ClientIdRouteValue + "}",
            Uri.EscapeDataString(clientId),
            StringComparison.Ordinal);

        try
        {
            using var response = await clients.CreateClient(ClientName).GetAsync(path, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ClientResponse>(cancellationToken)
                ?? throw new HttpRequestException("The control plane answered without a client.");
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ControlPlaneUnavailableException("The control plane could not be asked about the client.", exception);
        }
    }
}
