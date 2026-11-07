// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Domain.Usage;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

public sealed class UsageReaderTests
{
    // The fixture holds one event of 2026-11-04T13:30Z for each of its tenants.
    [Collection(MeteringDatabase.Collection)]
    public sealed class ReadAsync(MeteringDatabase database)
    {
        private static readonly DateTimeOffset Hour = DateTimeOffset.Parse("2026-11-04T13:00:00Z");

        [Fact]
        public async Task Returns_The_Hour_Of_The_Tenant()
        {
            var totals = await ReadForAsync(MeteringDatabase.TenantA, Query(Hour, Hour.AddHours(1), "hour"));

            Assert.Equal(new UsageTotal(Hour, "echo", "requests", 1), Assert.Single(totals));
        }

        [Fact]
        public async Task Returns_The_Day_Of_The_Tenant_At_Midnight_Utc()
        {
            var day = DateTimeOffset.Parse("2026-11-04T00:00:00Z");

            var totals = await ReadForAsync(MeteringDatabase.TenantA, Query(day, day.AddDays(1), "day"));

            Assert.Equal(new UsageTotal(day, "echo", "requests", 1), Assert.Single(totals));
        }

        [Fact]
        public async Task Returns_No_Hour_Of_Another_Tenant()
        {
            var tenantA = await ReadForAsync(MeteringDatabase.TenantA, Query(Hour, Hour.AddHours(1), "hour"));
            var tenantB = await ReadForAsync(MeteringDatabase.TenantB, Query(Hour, Hour.AddHours(1), "hour"));

            Assert.Equal((1L, 1L), (tenantA.Sum(total => total.Quantity), tenantB.Sum(total => total.Quantity)));
        }

        [Fact]
        public async Task Returns_Nothing_For_A_Tenant_Without_Usage()
        {
            var totals = await ReadForAsync(Guid.NewGuid(), Query(Hour, Hour.AddHours(1), "hour"));

            Assert.Empty(totals);
        }

        [Fact]
        public async Task Leaves_Out_An_Hour_That_Starts_Before_The_Period()
        {
            var totals = await ReadForAsync(MeteringDatabase.TenantA, Query(Hour.AddMinutes(30), Hour.AddHours(2), "hour"));

            Assert.Empty(totals);
        }

        [Fact]
        public async Task Leaves_Out_An_Hour_That_Starts_At_The_End_Of_The_Period()
        {
            var totals = await ReadForAsync(MeteringDatabase.TenantA, Query(Hour.AddHours(-1), Hour, "hour"));

            Assert.Empty(totals);
        }

        [Fact]
        public async Task Leaves_Out_Another_Module()
        {
            var totals = await ReadForAsync(MeteringDatabase.TenantA, Query(Hour, Hour.AddHours(1), "hour", moduleId: "other"));

            Assert.Empty(totals);
        }

        [Fact]
        public async Task Leaves_Out_Another_Meter()
        {
            var totals = await ReadForAsync(MeteringDatabase.TenantA, Query(Hour, Hour.AddHours(1), "hour", meter: "items"));

            Assert.Empty(totals);
        }

        // Without a tenant of its own: the reader works for the tenant it is asked for.
        private async Task<IReadOnlyList<UsageTotal>> ReadForAsync(Guid tenantId, UsageQuery query)
        {
            await using var scope = database.CreateScope(tenantId: null);
            return await scope.ServiceProvider.GetRequiredService<IUsageReader>().ReadAsync(tenantId, query, CancellationToken.None);
        }

        private static UsageQuery Query(DateTimeOffset from, DateTimeOffset to, string granularity, string? moduleId = null, string? meter = null)
        {
            return UsageQuery.Create(from, to, granularity, moduleId, meter).Value;
        }
    }
}
