// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Application.Tests.Unit;

public sealed class UsageRecordingTests
{
    private static readonly UsageEvent Usage = new(
        Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
        DateTimeOffset.Parse("2026-11-04T13:30:00Z"),
        Guid.Parse("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61"),
        "local-client",
        "echo",
        "/v1/echo",
        "requests",
        3,
        200,
        12,
        "0af7651916cd43dd8448eb211c80319c");

    public sealed class RecordAsync
    {
        [Fact]
        public async Task Stores_The_Event_With_Its_Hourly_And_Daily_Increment()
        {
            var store = new RecordingStore(isNew: true);

            await new UsageRecording(store).RecordAsync(Usage, CancellationToken.None);

            Assert.Equal((Usage, UsageAggregates.HourlyOf(Usage), UsageAggregates.DailyOf(Usage)), store.Recorded);
        }

        [Fact]
        public async Task Returns_True_For_A_New_Event()
        {
            var recorded = await new UsageRecording(new RecordingStore(isNew: true)).RecordAsync(Usage, CancellationToken.None);

            Assert.True(recorded);
        }

        [Fact]
        public async Task Returns_False_For_A_Duplicate()
        {
            var recorded = await new UsageRecording(new RecordingStore(isNew: false)).RecordAsync(Usage, CancellationToken.None);

            Assert.False(recorded);
        }
    }

    private sealed class RecordingStore(bool isNew) : IUsageStore
    {
        internal (UsageEvent, HourlyUsage, DailyUsage)? Recorded { get; private set; }

        public Task<bool> RecordAsync(UsageEvent usage, HourlyUsage hourly, DailyUsage daily, CancellationToken cancellationToken)
        {
            Recorded = (usage, hourly, daily);
            return Task.FromResult(isNew);
        }
    }
}
