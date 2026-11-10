// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;
using Homeport.Platform.Metering.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The cross-tenant leakage tests through EF Core (ADR-0005): with tenant A
/// set, no query returns a row of tenant B - neither through the query
/// filters nor, with them switched off, past row-level security. And that the
/// schema's ER diagram names every table and column of the model.
/// </summary>
public sealed class MeteringDbContextTests
{
    public sealed class Model
    {
        private const string Diagram = "data-model-metering.drawio.svg";

        [Fact]
        public void Names_Every_Table_And_Column_In_The_Er_Diagram()
        {
            using var context = new MeteringDesignTimeDbContextFactory().CreateDbContext([]);

            var missing = ErDiagram.Read(Diagram).MissingFrom(context);

            Assert.True(missing.Count == 0, $"{Diagram} lacks: {string.Join(", ", missing)}");
        }
    }

    [Collection(MeteringDatabase.Collection)]
    public sealed class UsageEvents(MeteringDatabase database)
    {
        [Fact]
        public async Task Returns_Only_The_Rows_Of_The_Current_Tenant()
        {
            await using var scope = database.CreateScope(MeteringDatabase.TenantA);

            var tenants = await Context(scope).UsageEvents.Select(u => u.TenantId).Distinct().ToListAsync();

            Assert.Equal([MeteringDatabase.TenantA], tenants);
        }

        // Switched off here only to show the second layer; outside a test that
        // is reserved for code paths marked as platform admin.
        [Fact]
        public async Task Returns_Only_The_Rows_Of_The_Current_Tenant_Without_The_Query_Filter()
        {
            await using var scope = database.CreateScope(MeteringDatabase.TenantA);

            var tenants = await Context(scope).UsageEvents.IgnoreQueryFilters().Select(u => u.TenantId).Distinct().ToListAsync();

            Assert.Equal([MeteringDatabase.TenantA], tenants);
        }

        [Fact]
        public async Task Returns_No_Rows_Without_A_Tenant()
        {
            await using var scope = database.CreateScope(tenantId: null);

            var count = await Context(scope).UsageEvents.IgnoreQueryFilters().CountAsync();

            Assert.Equal(0, count);
        }

        [Fact]
        public async Task Rejects_A_Row_Of_Another_Tenant()
        {
            await using var scope = database.CreateScope(MeteringDatabase.TenantA);
            var context = Context(scope);
            context.UsageEvents.Add(MeteringDatabase.EventOf(MeteringDatabase.TenantB));

            var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, Assert.IsType<PostgresException>(error.InnerException).SqlState);
        }
    }

    [Collection(MeteringDatabase.Collection)]
    public sealed class UsageHourly(MeteringDatabase database)
    {
        [Fact]
        public async Task Returns_Only_The_Rows_Of_The_Current_Tenant()
        {
            await using var scope = database.CreateScope(MeteringDatabase.TenantA);

            var tenants = await Context(scope).UsageHourly.Select(h => h.TenantId).Distinct().ToListAsync();

            Assert.Equal([MeteringDatabase.TenantA], tenants);
        }

        [Fact]
        public async Task Returns_Only_The_Rows_Of_The_Current_Tenant_Without_The_Query_Filter()
        {
            await using var scope = database.CreateScope(MeteringDatabase.TenantA);

            var tenants = await Context(scope).UsageHourly.IgnoreQueryFilters().Select(h => h.TenantId).Distinct().ToListAsync();

            Assert.Equal([MeteringDatabase.TenantA], tenants);
        }

        [Fact]
        public async Task Rejects_A_Row_Of_Another_Tenant()
        {
            await using var scope = database.CreateScope(MeteringDatabase.TenantA);
            var context = Context(scope);
            context.UsageHourly.Add(new HourlyUsage(MeteringDatabase.TenantB, "echo", "other", DateTimeOffset.Parse("2026-11-04T13:00:00Z"), 1));

            var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, Assert.IsType<PostgresException>(error.InnerException).SqlState);
        }
    }

    [Collection(MeteringDatabase.Collection)]
    public sealed class UsageDaily(MeteringDatabase database)
    {
        [Fact]
        public async Task Returns_Only_The_Rows_Of_The_Current_Tenant()
        {
            await using var scope = database.CreateScope(MeteringDatabase.TenantA);

            var tenants = await Context(scope).UsageDaily.Select(d => d.TenantId).Distinct().ToListAsync();

            Assert.Equal([MeteringDatabase.TenantA], tenants);
        }

        [Fact]
        public async Task Returns_Only_The_Rows_Of_The_Current_Tenant_Without_The_Query_Filter()
        {
            await using var scope = database.CreateScope(MeteringDatabase.TenantA);

            var tenants = await Context(scope).UsageDaily.IgnoreQueryFilters().Select(d => d.TenantId).Distinct().ToListAsync();

            Assert.Equal([MeteringDatabase.TenantA], tenants);
        }

        [Fact]
        public async Task Rejects_A_Row_Of_Another_Tenant()
        {
            await using var scope = database.CreateScope(MeteringDatabase.TenantA);
            var context = Context(scope);
            context.UsageDaily.Add(new DailyUsage(MeteringDatabase.TenantB, "echo", "other", new DateOnly(2026, 11, 4), 1));

            var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, Assert.IsType<PostgresException>(error.InnerException).SqlState);
        }
    }

    private static MeteringDbContext Context(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<MeteringDbContext>();
}
