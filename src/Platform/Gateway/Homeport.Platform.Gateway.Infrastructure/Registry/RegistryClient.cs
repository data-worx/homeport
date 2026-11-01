// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Homeport.Shared.Internal.Platform.Registry;

namespace Homeport.Platform.Gateway.Infrastructure.Registry;

/// <summary>Reads the route table from the registry, with a service token for it (ADR-0014).</summary>
public sealed class RegistryClient(IHttpClientFactory clients)
{
    /// <summary>The name of the HTTP client that calls the registry.</summary>
    internal const string ClientName = "Homeport.Registry";

    /// <summary>The route table, unless it is unchanged since <paramref name="etag"/>.</summary>
    /// <param name="etag">The <c>ETag</c> of the table read last; <c>null</c> on the first read.</param>
    /// <param name="cancellationToken">Aborts the read.</param>
    /// <returns><c>null</c> when the registry answered <c>304</c>.</returns>
    /// <exception cref="HttpRequestException">The registry could not be reached or answered with an error.</exception>
    public async Task<RouteTableRead?> ReadRoutesAsync(string? etag, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, RegistryService.RoutesPath);
        if (etag is not null)
        {
            request.Headers.IfNoneMatch.Add(EntityTagHeaderValue.Parse(etag));
        }

        using var response = await clients.CreateClient(ClientName).SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var table = await response.Content.ReadFromJsonAsync<RouteTableResponse>(cancellationToken)
            ?? throw new HttpRequestException("The registry answered without a route table.");

        return new RouteTableRead(table, response.Headers.ETag?.ToString());
    }
}
