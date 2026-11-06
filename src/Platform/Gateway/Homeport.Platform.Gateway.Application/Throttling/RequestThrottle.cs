// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Domain.Throttling;

namespace Homeport.Platform.Gateway.Application.Throttling;

/// <summary>
/// Decides whether a tenant's request passes its plan's rate limit. While the
/// buckets cannot be reached, every request passes: the gateway fails open
/// rather than refusing every tenant.
/// </summary>
public sealed class RequestThrottle(ITokenBuckets buckets)
{
    /// <summary>The decision for one request of <paramref name="tenantId"/>.</summary>
    /// <param name="tenantId">The tenant the request acts for.</param>
    /// <param name="limit">The rate limit of the tenant's plan.</param>
    /// <param name="cancellationToken">Aborts the decision.</param>
    /// <returns><c>null</c> when the buckets cannot be reached and the request passes unthrottled.</returns>
    public async Task<ThrottleDecision?> DecideAsync(Guid tenantId, RateLimit limit, CancellationToken cancellationToken)
    {
        var bucket = await buckets.TakeAsync(tenantId, limit, cancellationToken);

        return bucket.IsSuccess ? ThrottleDecision.From(limit, bucket.Value) : null;
    }
}
