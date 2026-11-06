// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Homeport.Platform.Gateway.Infrastructure.ApiDocs;

/// <summary>
/// Reads the OpenAPI document of a module instance from the URL the registry
/// names for it (ADR-0017).
/// </summary>
/// <remarks>
/// Without a token: a module serves its document anonymously, as the module
/// contract prescribes, so the gateway has nothing to prove to read it.
/// </remarks>
public sealed class ModuleDocumentsClient(IHttpClientFactory clients)
{
    /// <summary>The name of the HTTP client that reads the documents.</summary>
    internal const string ClientName = "Homeport.ModuleDocuments";

    /// <summary>The document at <paramref name="url"/>.</summary>
    /// <param name="url">Where the instance serves its document.</param>
    /// <param name="cancellationToken">Aborts the read.</param>
    /// <exception cref="HttpRequestException">The instance could not be reached or answered with an error.</exception>
    /// <exception cref="JsonException">The instance answered with something other than a JSON object.</exception>
    public async Task<JsonObject> ReadAsync(Uri url, CancellationToken cancellationToken)
    {
        var document = await clients.CreateClient(ClientName).GetFromJsonAsync<JsonNode>(url, cancellationToken);

        return document as JsonObject ?? throw new JsonException($"The document at {url} is not a JSON object.");
    }
}
