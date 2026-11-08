// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Homeport.Platform.Gateway.Infrastructure.Signing;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Homeport.Platform.Gateway.Infrastructure.ApiDocs;

/// <summary>
/// Reads the OpenAPI document of a platform service for the platform's API
/// docs (ADR-0017), with a service token for that service (ADR-0014): unlike
/// a module, a platform service serves its document to the gateway only.
/// </summary>
public sealed class PlatformDocumentsClient(IHttpClientFactory clients, ServiceTokenIssuer issuer)
{
    /// <summary>The name of the HTTP client that reads the documents.</summary>
    internal const string ClientName = "Homeport.PlatformDocuments";

    /// <summary>The document at <paramref name="url"/>.</summary>
    /// <param name="url">Where the service serves its document.</param>
    /// <param name="audience">The audience of the service's tokens.</param>
    /// <param name="cancellationToken">Aborts the read.</param>
    /// <exception cref="HttpRequestException">The service could not be reached or answered with an error.</exception>
    /// <exception cref="JsonException">The service answered with something other than a JSON object.</exception>
    public async Task<JsonObject> ReadAsync(Uri url, string audience, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, issuer.Issue(audience));

        using var response = await clients.CreateClient(ClientName).SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var document = await response.Content.ReadFromJsonAsync<JsonNode>(cancellationToken);

        return document as JsonObject ?? throw new JsonException($"The document at {url} is not a JSON object.");
    }
}
