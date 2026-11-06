// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Domain.Throttling;

namespace Homeport.Platform.Gateway.Domain.Tests.Unit.Throttling;

public sealed class RateLimitTests
{
    public sealed class Constructor
    {
        [Theory]
        [InlineData(0, 20)]
        [InlineData(-1, 20)]
        [InlineData(10, 0)]
        [InlineData(10, -1)]
        public void Rejects_A_Value_Below_One(int requestsPerSecond, int burst)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RateLimit(requestsPerSecond, burst));
        }
    }

    public sealed class FillTime
    {
        [Fact]
        public void Is_The_Burst_Divided_By_The_Rate()
        {
            var limit = new RateLimit(10, 25);

            Assert.Equal(TimeSpan.FromSeconds(2.5), limit.FillTime);
        }
    }
}
