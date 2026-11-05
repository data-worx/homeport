// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Domain.Tests.Unit.Usage;

public sealed class UsageBucketsTests
{
    public sealed class HourOf
    {
        [Fact]
        public void Returns_The_Start_Of_The_Hour()
        {
            var hour = UsageBuckets.HourOf(DateTimeOffset.Parse("2026-11-04T13:59:59.999Z"));

            Assert.Equal(DateTimeOffset.Parse("2026-11-04T13:00:00Z"), hour);
        }

        [Fact]
        public void Returns_The_Next_Hour_At_The_Full_Hour()
        {
            var hour = UsageBuckets.HourOf(DateTimeOffset.Parse("2026-11-04T14:00:00Z"));

            Assert.Equal(DateTimeOffset.Parse("2026-11-04T14:00:00Z"), hour);
        }

        [Fact]
        public void Returns_The_Hour_In_Utc_When_The_Moment_Has_An_Offset()
        {
            var hour = UsageBuckets.HourOf(DateTimeOffset.Parse("2026-11-04T14:30:00+01:00"));

            Assert.Equal(TimeSpan.Zero, hour.Offset);
            Assert.Equal(DateTimeOffset.Parse("2026-11-04T13:00:00Z"), hour);
        }
    }

    public sealed class DayOf
    {
        [Fact]
        public void Returns_Different_Days_Either_Side_Of_Midnight()
        {
            var before = UsageBuckets.DayOf(DateTimeOffset.Parse("2026-11-04T23:59:59.999Z"));
            var after = UsageBuckets.DayOf(DateTimeOffset.Parse("2026-11-05T00:00:00Z"));

            Assert.Equal(new DateOnly(2026, 11, 4), before);
            Assert.Equal(new DateOnly(2026, 11, 5), after);
        }

        [Fact]
        public void Returns_The_Day_In_Utc_When_The_Moment_Has_An_Offset()
        {
            var day = UsageBuckets.DayOf(DateTimeOffset.Parse("2026-11-05T00:30:00+01:00"));

            Assert.Equal(new DateOnly(2026, 11, 4), day);
        }
    }
}
