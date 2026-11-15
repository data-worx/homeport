// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Infrastructure.Persistence;
using Npgsql;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// The audit entries are append-only for both roles metering connects as
/// (ADR-0020): each may add and read them, neither may change or delete one.
/// </summary>
public sealed class AuditPrivilegesTests
{
    private const string AuditEntries = $"{MeteringDbContext.Schema}.\"{MeteringDbContext.AuditEntriesTable}\"";

    private const string App = "app";

    private const string Admin = "admin";

    [Collection(MeteringDatabase.Collection)]
    public sealed class Restrict(MeteringDatabase database)
    {
        [Theory]
        [InlineData(App)]
        [InlineData(Admin)]
        public async Task Lets_The_Role_Add_And_Read_Audit_Entries(string role)
        {
            var id = Guid.NewGuid();
            await MeteringDatabase.ExecuteScalarAsync(
                ConnectionStringOf(role),
                $"INSERT INTO {AuditEntries} (\"Id\", \"OccurredAt\", \"OperatorSubject\", \"OperatorName\", \"Action\", \"Target\", \"Summary\") "
                + "VALUES ($1, now(), 'subject', 'name', 'test.inserted', 'tests/1', 'Inserted by a test.')",
                id);

            var count = await MeteringDatabase.ExecuteScalarAsync(ConnectionStringOf(role), $"SELECT count(*) FROM {AuditEntries} WHERE \"Id\" = $1", id);

            Assert.Equal(1L, count);
        }

        [Theory]
        [InlineData(App)]
        [InlineData(Admin)]
        public async Task Keeps_The_Role_From_Updating_Audit_Entries(string role)
        {
            var error = await Assert.ThrowsAsync<PostgresException>(
                () => MeteringDatabase.ExecuteScalarAsync(ConnectionStringOf(role), $"UPDATE {AuditEntries} SET \"Summary\" = 'changed'"));

            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        }

        [Theory]
        [InlineData(App)]
        [InlineData(Admin)]
        public async Task Keeps_The_Role_From_Deleting_Audit_Entries(string role)
        {
            var error = await Assert.ThrowsAsync<PostgresException>(
                () => MeteringDatabase.ExecuteScalarAsync(ConnectionStringOf(role), $"DELETE FROM {AuditEntries}"));

            Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
        }

        private string ConnectionStringOf(string role) => role == Admin ? database.AdminConnectionString : database.AppConnectionString;
    }
}
