// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>
/// Sends every request of an HTTP client with a service token for the platform
/// service it calls. Added to a client with
/// <see cref="SigningServiceCollectionExtensions.AddServiceToken"/>.
/// </summary>
internal sealed class ServiceTokenHandler(ServiceTokenIssuer issuer, string audience) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(
            JwtBearerDefaults.AuthenticationScheme,
            issuer.Issue(audience));

        return base.SendAsync(request, cancellationToken);
    }
}
