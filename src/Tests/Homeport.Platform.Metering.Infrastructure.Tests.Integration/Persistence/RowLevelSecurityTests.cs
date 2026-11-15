// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Homeport.Platform.Metering.Infrastructure.Persistence;
using Npgsql;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The cross-tenant leakage tests in raw SQL as the app role, past EF Core
/// (ADR-0005): row-level security alone keeps tenant B's rows from tenant A.
/// The admin role reads every tenant's aggregates and changes nothing
/// (ADR-0020).
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

    [Collection(MeteringDatabase.Collection)]
    public sealed class EnableAdminRead(MeteringDatabase database)
    {
        [Theory]
        [InlineData(MeteringDbContext.UsageHourlyTable)]
        [InlineData(MeteringDbContext.UsageDailyTable)]
        public async Task Shows_The_Admin_Role_The_Rows_Of_Every_Tenant(string table)
        {
            var count = await MeteringDatabase.ExecuteScalarAsync(
                database.AdminConnectionString,
                $"SELECT count(DISTINCT \"TenantId\") FROM {MeteringDbContext.Schema}.\"{table}\" WHERE \"TenantId\" IN ($1, $2)",
                MeteringDatabase.TenantA,
                MeteringDatabase.TenantB);

            Assert.Equal(2L, count);
        }

        [Theory]
        [InlineData(MeteringDbContext.UsageHourlyTable, "UPDATE {0} SET \"Quantity\" = \"Quantity\" + 1")]
        [InlineData(MeteringDbContext.UsageDailyTable, "UPDATE {0} SET \"Quantity\" = \"Quantity\" + 1")]
        [InlineData(MeteringDbContext.UsageHourlyTable, "DELETE FROM {0}")]
        [InlineData(MeteringDbContext.UsageDailyTable, "DELETE FROM {0}")]
        [InlineData(MeteringDbContext.UsageHourlyTable, "INSERT INTO {0} SELECT * FROM {0} LIMIT 0")]
        [InlineData(MeteringDbContext.UsageDailyTable, "INSERT INTO {0} SELECT * FROM {0} LIMIT 0")]
        public async Task Keeps_The_Admin_Role_From_Writing_The_Aggregates(string table, string statement)
        {
            var sql = string.Format(CultureInfo.InvariantCulture, statement, $"{MeteringDbContext.Schema}.\"{table}\"");

            var error = await Assert.ThrowsAsync<PostgresException>(() => MeteringDatabase.ExecuteScalarAsync(database.AdminConnectionString, sql));

            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        }

        [Fact]
        public async Task Keeps_The_Admin_Role_From_Reading_The_Usage_Events()
        {
            var error = await Assert.ThrowsAsync<PostgresException>(
                () => MeteringDatabase.ExecuteScalarAsync(database.AdminConnectionString, $"SELECT count(*) FROM {MeteringDbContext.Schema}.\"{MeteringDbContext.UsageEventsTable}\""));

            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        }
    }
}
