// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Shared.Authentication;

/// <summary>
/// Reads a bare JWKS into the configuration JWT bearer validates against. The
/// gateway publishes no OpenID Connect discovery document, only its keys.
/// </summary>
/// <remarks>
/// Fetches through <see cref="IHttpClientFactory"/> rather than the document
/// retriever it is handed, so service discovery resolves the gateway's address
/// and the client follows the service defaults.
/// </remarks>
internal sealed class JsonWebKeySetRetriever(IHttpClientFactory clients, ILogger<JsonWebKeySetRetriever> logger) : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    /// <summary>The named client the JWKS is fetched with.</summary>
    internal const string ClientName = "homeport-gateway-jwks";

    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address,
        IDocumentRetriever retriever,
        CancellationToken cancel)
    {
        try
        {
            using var client = clients.CreateClient(ClientName);
            var json = await client.GetStringAsync(new Uri(address), cancel);

            var configuration = new OpenIdConnectConfiguration();
            foreach (var key in new JsonWebKeySet(json).GetSigningKeys())
            {
                configuration.SigningKeys.Add(key);
            }

            return configuration;
        }
        catch (Exception exception) when (!cancel.IsCancellationRequested)
        {
            // Logged here and rethrown, an exception to core.logging: the
            // configuration manager handles the failure, but keeps validating
            // against the keys it has and writes nothing to ILogger, so a
            // failed refresh would otherwise go unnoticed.
            logger.LogWarning(exception, "The gateway's JWKS could not be retrieved from {JwksUri}", address);
            throw;
        }
    }
}
