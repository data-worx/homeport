// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Infrastructure.Persistence;
using Npgsql;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The cross-tenant leakage tests in raw SQL as the app role, past EF Core
/// (ADR-0005): row-level security alone keeps tenant B's rows from tenant A.
/// </summary>
public sealed class RowLevelSecurityTests
{
    [Collection(MeteringDatabase.Collection)]
    public sealed class Enable(MeteringDatabase database)
    {
        [Theory]
        [InlineData(MeteringDbContext.UsageEventsTable)]
        [InlineData(MeteringDbContext.UsageHourlyTable)]
        [InlineData(MeteringDbContext.UsageDailyTable)]
        public async Task Hides_The_Rows_Of_Another_Tenant(string table)
        {
            var count = await CountAsAppRoleAsync(table, MeteringDatabase.TenantA.ToString(), MeteringDatabase.TenantB);

            Assert.Equal(0L, count);
        }

        [Theory]
        [InlineData(MeteringDbContext.UsageEventsTable)]
        [InlineData(MeteringDbContext.UsageHourlyTable)]
        [InlineData(MeteringDbContext.UsageDailyTable)]
        public async Task Shows_The_Rows_Of_The_Session_Tenant(string table)
        {
            var count = await CountAsAppRoleAsync(table, MeteringDatabase.TenantA.ToString(), MeteringDatabase.TenantA);

            Assert.Equal(1L, count);
        }

        [Theory]
        [InlineData(MeteringDbContext.UsageEventsTable)]
        [InlineData(MeteringDbContext.UsageHourlyTable)]
        [InlineData(MeteringDbContext.UsageDailyTable)]
        public async Task Hides_Every_Row_Without_A_Session_Tenant(string table)
        {
            var count = await CountAsAppRoleAsync(table, sessionTenant: null, rowTenant: null);

            Assert.Equal(0L, count);
        }

        // The table name comes from the test data, never from input; the
        // tenants are parameters.
        private async Task<object?> CountAsAppRoleAsync(string table, string? sessionTenant, Guid? rowTenant)
        {
            await using var connection = new NpgsqlConnection(database.AppConnectionString);
            await connection.OpenAsync();

            if (sessionTenant is not null)
            {
                await using var set = new NpgsqlCommand($"SELECT set_config('{RowLevelSecurity.TenantSetting}', @tenant, false)", connection);
                set.Parameters.AddWithValue("tenant", sessionTenant);
                await set.ExecuteNonQueryAsync();
            }

            var filter = rowTenant is null ? string.Empty : " WHERE \"TenantId\" = @row";
            await using var count = new NpgsqlCommand($"SELECT count(*) FROM {MeteringDbContext.Schema}.\"{table}\"{filter}", connection);
            if (rowTenant is not null)
            {
                count.Parameters.AddWithValue("row", rowTenant.Value);
            }

            return await count.ExecuteScalarAsync();
        }
    }
}
