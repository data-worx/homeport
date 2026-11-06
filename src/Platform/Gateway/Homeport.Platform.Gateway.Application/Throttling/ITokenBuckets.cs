// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Domain.Throttling;
using Homeport.Shared.Contracts;

namespace Homeport.Platform.Gateway.Application.Throttling;

/// <summary>
/// The token buckets of every tenant, shared by every gateway replica, so a
/// limit holds however many of them a tenant's requests reach.
/// </summary>
public interface ITokenBuckets
{
    /// <summary>The code of the error returned while the buckets cannot be reached.</summary>
    const string UnavailableCode = "throttling.unavailable";

    /// <summary>Takes one token from <paramref name="tenantId"/>'s bucket, sized by <paramref name="limit"/>.</summary>
    /// <param name="tenantId">The tenant whose bucket is drawn from.</param>
    /// <param name="limit">The rate limit of the tenant's plan.</param>
    /// <param name="cancellationToken">Aborts the attempt.</param>
    /// <returns>The bucket after the attempt, or an error with <see cref="UnavailableCode"/>.</returns>
    Task<Result<BucketState>> TakeAsync(Guid tenantId, RateLimit limit, CancellationToken cancellationToken);
}
