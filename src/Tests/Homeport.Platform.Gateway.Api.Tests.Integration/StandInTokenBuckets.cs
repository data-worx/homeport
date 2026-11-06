// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Application.Throttling;
using Homeport.Platform.Gateway.Domain.Throttling;
using Homeport.Shared.Contracts;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>
/// Token buckets in memory that answer every request with
/// <see cref="Next"/>: a full bucket unless a test says otherwise. The
/// buckets themselves are tested against Valkey in the gateway's
/// Infrastructure tests.
/// </summary>
internal sealed class StandInTokenBuckets : ITokenBuckets
{
    /// <summary>A bucket that handed out a token and has more left.</summary>
    internal static readonly BucketState Full = new(true, 19, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));

    /// <summary>A bucket without a token, which holds one again in 1.5 seconds.</summary>
    internal static readonly BucketState Empty = new(false, 0, TimeSpan.FromMilliseconds(1500), TimeSpan.FromSeconds(2));

    /// <summary>What the next request draws.</summary>
    internal BucketState Next { get; set; } = Full;

    public Task<Result<BucketState>> TakeAsync(Guid tenantId, RateLimit limit, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<BucketState>.Success(Next));
    }
}
