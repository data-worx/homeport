// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;

namespace Homeport.Aspire.AppHost.Tests.Integration;

/// <summary>
/// Sends a request again after the <c>Retry-After</c> of a <c>429</c>, until
/// <see cref="Within"/> has passed. Every test calls Echo as the seeded
/// tenant, whose small plan the throttling tests use up; the others wait for
/// the gateway's signal that a token is free rather than fail on it.
/// </summary>
internal sealed class RetryAfterHandler : DelegatingHandler
{
    /// <summary>How long a request is retried at most.</summary>
    internal static readonly TimeSpan Within = TimeSpan.FromSeconds(30);

    public RetryAfterHandler()
        : base(new HttpClientHandler())
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var deadline = TimeProvider.System.GetUtcNow() + Within;
        var response = await base.SendAsync(request, cancellationToken);
        while (response.StatusCode == HttpStatusCode.TooManyRequests
            && response.Headers.RetryAfter?.Delta is { } retryAfter
            && TimeProvider.System.GetUtcNow() + retryAfter < deadline)
        {
            response.Dispose();
            await Task.Delay(retryAfter, cancellationToken);
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }
}
