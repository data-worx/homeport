// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Npgsql;

namespace Homeport.Platform.ControlPlane.Api.Tests.Integration;

/// <summary>
/// The roles <c>deploy/postgres/init/init-homeport.sh</c> creates: the service
/// connects as an app role that reads and writes its tables but cannot change
/// them.
/// </summary>
public sealed class DatabaseRolesTests(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
{
    private const string AuditEntries = $"{ControlPlaneDbContext.Schema}.\"{ControlPlaneDbContext.AuditEntriesTable}\"";

    [Fact]
    public async Task App_Role_Reads_The_Tables_Of_Its_Schema()
    {
        var count = await controlPlane.ExecuteScalarAsAppRoleAsync($"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"Clients\"");

        Assert.Equal(1L, count);
    }

    [Fact]
    public async Task App_Role_Cannot_Create_A_Table_In_Its_Schema()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(
            () => controlPlane.ExecuteScalarAsAppRoleAsync($"CREATE TABLE {ControlPlaneDbContext.Schema}.\"Intruders\" (\"Id\" uuid)"));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task App_Role_Adds_And_Reads_Audit_Entries()
    {
        var id = Guid.NewGuid();
        await controlPlane.ExecuteScalarAsAppRoleAsync(
            $"INSERT INTO {AuditEntries} (\"Id\", \"OccurredAt\", \"OperatorSubject\", \"OperatorName\", \"Action\", \"Target\", \"Summary\") "
            + "VALUES ($1, now(), 'subject', 'name', 'test.inserted', 'tests/1', 'Inserted by a test.')",
            id);

        var count = await controlPlane.ExecuteScalarAsAppRoleAsync($"SELECT count(*) FROM {AuditEntries} WHERE \"Id\" = $1", id);

        Assert.Equal(1L, count);
    }

    [Fact]
    public async Task App_Role_Cannot_Update_Audit_Entries()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(
            () => controlPlane.ExecuteScalarAsAppRoleAsync($"UPDATE {AuditEntries} SET \"Summary\" = 'changed'"));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task App_Role_Cannot_Delete_Audit_Entries()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(
            () => controlPlane.ExecuteScalarAsAppRoleAsync($"DELETE FROM {AuditEntries}"));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task App_Role_Cannot_Drop_A_Table_Of_Its_Schema()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(
            () => controlPlane.ExecuteScalarAsAppRoleAsync($"DROP TABLE {ControlPlaneDbContext.Schema}.\"Clients\""));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }
}
