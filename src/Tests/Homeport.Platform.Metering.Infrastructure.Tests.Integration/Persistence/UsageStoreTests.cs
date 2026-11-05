// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Domain.Usage;
using Homeport.Platform.Metering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

public sealed class UsageStoreTests
{
    // Each test records for a tenant of its own, so the fixture's tenants and
    // the other tests see none of it.
    [Collection(MeteringDatabase.Collection)]
    public sealed class RecordAsync(MeteringDatabase database)
    {
        [Fact]
        public async Task Returns_True_For_A_New_Event()
        {
            var usage = MeteringDatabase.EventOf(Guid.NewGuid());

            var recorded = await StoreAsync(usage);

            Assert.True(recorded);
        }

        [Fact]
        public async Task Returns_False_For_An_Event_Recorded_Before()
        {
            var usage = MeteringDatabase.EventOf(Guid.NewGuid());
            await StoreAsync(usage);

            var recorded = await StoreAsync(usage);

            Assert.False(recorded);
        }

        [Fact]
        public async Task Creates_The_Hourly_And_Daily_Aggregate_Of_A_First_Event()
        {
            var usage = MeteringDatabase.EventOf(Guid.NewGuid());

            await StoreAsync(usage);

            Assert.Equal((1L, 1L), await QuantitiesAsync(usage.TenantId));
        }

        [Fact]
        public async Task Adds_To_The_Aggregates_Of_An_Existing_Bucket()
        {
            var tenantId = Guid.NewGuid();
            await StoreAsync(MeteringDatabase.EventOf(tenantId));

            await StoreAsync(MeteringDatabase.EventOf(tenantId));

            Assert.Equal((2L, 2L), await QuantitiesAsync(tenantId));
        }

        [Fact]
        public async Task Leaves_The_Aggregates_Unchanged_For_An_Event_Recorded_Before()
        {
            var usage = MeteringDatabase.EventOf(Guid.NewGuid());
            await StoreAsync(usage);

            await StoreAsync(usage);

            Assert.Equal((1L, 1L), await QuantitiesAsync(usage.TenantId));
        }

        // Without a tenant of its own: the store works for the event's tenant.
        private async Task<bool> StoreAsync(UsageEvent usage)
        {
            await using var scope = database.CreateScope(tenantId: null);
            return await scope.ServiceProvider.GetRequiredService<IUsageStore>()
                .RecordAsync(usage, UsageAggregates.HourlyOf(usage), UsageAggregates.DailyOf(usage), CancellationToken.None);
        }

        private async Task<(long Hourly, long Daily)> QuantitiesAsync(Guid tenantId)
        {
            await using var scope = database.CreateScope(tenantId);
            var context = scope.ServiceProvider.GetRequiredService<MeteringDbContext>();

            return (await context.UsageHourly.SumAsync(h => h.Quantity), await context.UsageDaily.SumAsync(d => d.Quantity));
        }
    }
}
