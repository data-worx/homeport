// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Domain.Tests.Unit.Usage;

public sealed class UsageAggregatesTests
{
    private static readonly Guid Tenant = Guid.Parse("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61");

    private static readonly UsageEvent Usage = new(
        Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e"),
        DateTimeOffset.Parse("2026-11-04T23:59:59.999Z"),
        Tenant,
        "local-client",
        "echo",
        "/v1/echo",
        "requests",
        3,
        200,
        12,
        "0af7651916cd43dd8448eb211c80319c");

    public sealed class HourlyOf
    {
        [Fact]
        public void Adds_The_Quantity_To_The_Hour_Of_The_Event()
        {
            var hourly = UsageAggregates.HourlyOf(Usage);

            Assert.Equal(new HourlyUsage(Tenant, "echo", "requests", DateTimeOffset.Parse("2026-11-04T23:00:00Z"), 3), hourly);
        }
    }

    public sealed class DailyOf
    {
        [Fact]
        public void Adds_The_Quantity_To_The_Day_Of_The_Event()
        {
            var daily = UsageAggregates.DailyOf(Usage);

            Assert.Equal(new DailyUsage(Tenant, "echo", "requests", new DateOnly(2026, 11, 4), 3), daily);
        }
    }
}
