// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Domain.Tests.Unit.Usage;

public sealed class UsageQueryTests
{
    public sealed class Create
    {
        private static readonly DateTimeOffset From = DateTimeOffset.Parse("2026-11-04T00:00:00Z");

        private static readonly DateTimeOffset To = DateTimeOffset.Parse("2026-11-05T00:00:00Z");

        [Theory]
        [InlineData("hour", UsageGranularity.Hour)]
        [InlineData("day", UsageGranularity.Day)]
        public void Returns_The_Granularity_It_Names(string name, UsageGranularity expected)
        {
            var query = UsageQuery.Create(From, To, name, moduleId: null, meter: null);

            Assert.Equal(expected, query.Value.Granularity);
        }

        [Theory]
        [InlineData("week")]
        [InlineData("Hour")]
        [InlineData("")]
        public void Returns_Failure_For_An_Unknown_Granularity(string name)
        {
            var query = UsageQuery.Create(From, To, name, moduleId: null, meter: null);

            Assert.Equal(UsageQuery.UnknownGranularityCode, query.Error.Code);
        }

        [Fact]
        public void Returns_Failure_When_The_Period_Ends_Before_It_Starts()
        {
            var query = UsageQuery.Create(To, From, "hour", moduleId: null, meter: null);

            Assert.Equal(UsageQuery.EmptyPeriodCode, query.Error.Code);
        }

        [Fact]
        public void Returns_Failure_When_The_Period_Is_Empty()
        {
            var query = UsageQuery.Create(From, From, "hour", moduleId: null, meter: null);

            Assert.Equal(UsageQuery.EmptyPeriodCode, query.Error.Code);
        }

        [Fact]
        public void Returns_The_Bounds_In_Utc_When_They_Have_An_Offset()
        {
            var query = UsageQuery.Create(
                DateTimeOffset.Parse("2026-11-04T01:00:00+01:00"),
                DateTimeOffset.Parse("2026-11-04T02:00:00+01:00"),
                "hour",
                moduleId: null,
                meter: null);

            Assert.Equal((TimeSpan.Zero, TimeSpan.Zero), (query.Value.From.Offset, query.Value.To.Offset));
            Assert.Equal((From, From.AddHours(1)), (query.Value.From, query.Value.To));
        }

        [Fact]
        public void Returns_No_Module_And_No_Meter_When_They_Are_Empty()
        {
            var query = UsageQuery.Create(From, To, "hour", moduleId: string.Empty, meter: string.Empty);

            Assert.Equal((null, null), (query.Value.ModuleId, query.Value.Meter));
        }
    }

    public sealed class FirstDay
    {
        [Fact]
        public void Returns_The_Day_Of_A_Period_That_Starts_At_Midnight()
        {
            var query = DayQuery("2026-11-04T00:00:00Z", "2026-11-06T00:00:00Z");

            Assert.Equal(new DateOnly(2026, 11, 4), query.FirstDay);
        }

        [Fact]
        public void Returns_The_Next_Day_Of_A_Period_That_Starts_After_Midnight()
        {
            var query = DayQuery("2026-11-04T00:00:01Z", "2026-11-06T00:00:00Z");

            Assert.Equal(new DateOnly(2026, 11, 5), query.FirstDay);
        }
    }

    public sealed class EndDay
    {
        [Fact]
        public void Returns_The_Day_A_Period_Ends_At_At_Midnight()
        {
            var query = DayQuery("2026-11-04T00:00:00Z", "2026-11-06T00:00:00Z");

            Assert.Equal(new DateOnly(2026, 11, 6), query.EndDay);
        }

        [Fact]
        public void Returns_The_Next_Day_Of_A_Period_That_Ends_After_Midnight()
        {
            var query = DayQuery("2026-11-04T00:00:00Z", "2026-11-06T12:00:00Z");

            Assert.Equal(new DateOnly(2026, 11, 7), query.EndDay);
        }
    }

    private static UsageQuery DayQuery(string from, string to)
    {
        return UsageQuery.Create(DateTimeOffset.Parse(from), DateTimeOffset.Parse(to), "day", moduleId: null, meter: null).Value;
    }
}
