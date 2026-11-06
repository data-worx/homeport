// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Domain.Throttling;

namespace Homeport.Platform.Gateway.Domain.Tests.Unit.Throttling;

public sealed class ThrottleDecisionTests
{
    public sealed class From
    {
        private static readonly RateLimit Limit = new(10, 20);

        [Fact]
        public void Allows_A_Request_That_Took_A_Token()
        {
            var decision = ThrottleDecision.From(Limit, new BucketState(true, 19, TimeSpan.Zero, TimeSpan.FromMilliseconds(100)));

            Assert.Equal((true, 0), (decision.Allowed, decision.RetryAfterSeconds));
        }

        [Fact]
        public void Rejects_A_Request_That_Took_No_Token()
        {
            var decision = ThrottleDecision.From(Limit, new BucketState(false, 0, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2)));

            Assert.False(decision.Allowed);
        }

        [Fact]
        public void Rounds_The_Retry_After_Up_To_Whole_Seconds()
        {
            var decision = ThrottleDecision.From(Limit, new BucketState(false, 0, TimeSpan.FromMilliseconds(1100), TimeSpan.FromSeconds(2)));

            Assert.Equal(2, decision.RetryAfterSeconds);
        }

        [Fact]
        public void Tells_A_Rejected_Request_To_Retry_After_At_Least_One_Second()
        {
            var decision = ThrottleDecision.From(Limit, new BucketState(false, 0, TimeSpan.Zero, TimeSpan.FromSeconds(2)));

            Assert.Equal(1, decision.RetryAfterSeconds);
        }

        [Fact]
        public void States_The_Burst_As_The_Quota_Of_The_Fill_Time()
        {
            var decision = ThrottleDecision.From(new RateLimit(10, 25), new BucketState(true, 24, TimeSpan.Zero, TimeSpan.FromMilliseconds(100)));

            Assert.Equal((25, 3), (decision.Quota, decision.WindowSeconds));
        }

        [Fact]
        public void States_A_Window_Of_At_Least_One_Second()
        {
            var decision = ThrottleDecision.From(new RateLimit(100, 10), new BucketState(true, 9, TimeSpan.Zero, TimeSpan.FromMilliseconds(10)));

            Assert.Equal(1, decision.WindowSeconds);
        }

        [Fact]
        public void States_The_Remaining_Tokens_And_The_Seconds_Until_Full()
        {
            var decision = ThrottleDecision.From(Limit, new BucketState(true, 7, TimeSpan.Zero, TimeSpan.FromMilliseconds(1300)));

            Assert.Equal((7, 2), (decision.Remaining, decision.ResetSeconds));
        }

        [Fact]
        public void States_No_More_Remaining_Than_The_Bucket_Holds()
        {
            // A plan that shrank leaves more tokens in the bucket than it now holds.
            var decision = ThrottleDecision.From(Limit, new BucketState(true, 50, TimeSpan.Zero, TimeSpan.Zero));

            Assert.Equal(20, decision.Remaining);
        }
    }
}
