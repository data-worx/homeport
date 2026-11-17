// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Infrastructure.Persistence;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// Upgrading from the last release keeps every row it wrote: the migrations
/// since then neither drop nor change a column the release knew.
/// </summary>
public sealed class UpgradeTests
{
    private const string Schema = MeteringDbContext.Schema;

    public sealed class FromV0_1_0(UpgradedDatabase database) : IClassFixture<UpgradedDatabase>
    {
        [Fact]
        public async Task Keeps_The_Event()
        {
            var row = await database.ReadRowAsync(
                $"""
                SELECT "OccurredAt", "TenantId", "ClientId", "ModuleId", "Route", "Meter", "Quantity", "StatusCode", "DurationMs", "TraceId"
                FROM {Schema}."UsageEvents" WHERE "EventId" = $1
                """,
                UpgradedDatabase.EventId);

            Assert.Equal(
                [
                    UpgradedDatabase.OccurredAt, UpgradedDatabase.TenantId, UpgradedDatabase.ClientId, UpgradedDatabase.ModuleId,
                    UpgradedDatabase.Route, UpgradedDatabase.Meter, UpgradedDatabase.Quantity, UpgradedDatabase.StatusCode,
                    UpgradedDatabase.DurationMs, UpgradedDatabase.TraceId,
                ],
                row);
        }

        [Fact]
        public async Task Keeps_The_Hourly_Aggregate()
        {
            var row = await database.ReadRowAsync(
                $"""SELECT "ModuleId", "Meter", "Hour", "Quantity" FROM {Schema}."UsageHourly" WHERE "TenantId" = $1""",
                UpgradedDatabase.TenantId);

            Assert.Equal([UpgradedDatabase.ModuleId, UpgradedDatabase.Meter, UpgradedDatabase.Hour, (long)UpgradedDatabase.Quantity], row);
        }

        [Fact]
        public async Task Keeps_The_Daily_Aggregate()
        {
            var row = await database.ReadRowAsync(
                $"""SELECT "ModuleId", "Meter", "Day", "Quantity" FROM {Schema}."UsageDaily" WHERE "TenantId" = $1""",
                UpgradedDatabase.TenantId);

            Assert.Equal([UpgradedDatabase.ModuleId, UpgradedDatabase.Meter, UpgradedDatabase.Day, (long)UpgradedDatabase.Quantity], row);
        }
    }
}
