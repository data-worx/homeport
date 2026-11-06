// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Application.Throttling;
using Homeport.Platform.Gateway.Domain.Throttling;
using Homeport.Shared.Contracts;

namespace Homeport.Platform.Gateway.Application.Tests.Unit.Throttling;

public sealed class RequestThrottleTests
{
    public sealed class DecideAsync
    {
        private static readonly RateLimit Limit = new(10, 20);

        [Fact]
        public async Task Decides_On_The_Bucket_The_Request_Drew_From()
        {
            var throttle = new RequestThrottle(new FixedBuckets(Result<BucketState>.Success(
                new BucketState(false, 0, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2)))));

            var decision = await throttle.DecideAsync(Guid.NewGuid(), Limit, CancellationToken.None);

            Assert.Equal((false, 1), (decision?.Allowed, decision?.RetryAfterSeconds));
        }

        [Fact]
        public async Task Decides_Nothing_While_The_Buckets_Are_Unavailable()
        {
            var throttle = new RequestThrottle(new FixedBuckets(Result<BucketState>.Failure(
                new Error(ITokenBuckets.UnavailableCode, "Unavailable."))));

            var decision = await throttle.DecideAsync(Guid.NewGuid(), Limit, CancellationToken.None);

            Assert.Null(decision);
        }

        private sealed class FixedBuckets(Result<BucketState> bucket) : ITokenBuckets
        {
            public Task<Result<BucketState>> TakeAsync(Guid tenantId, RateLimit limit, CancellationToken cancellationToken)
            {
                return Task.FromResult(bucket);
            }
        }
    }
}
