// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Persistence;
using Npgsql;

namespace Homeport.Platform.ControlPlane.Tests.Integration;

/// <summary>
/// The roles <c>deploy/postgres/init/init-homeport.sh</c> creates: the service
/// connects as an app role that reads and writes its tables but cannot change
/// them.
/// </summary>
public sealed class DatabaseRolesTests(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
{
    [Fact]
    public async Task App_Role_Reads_The_Tables_Of_Its_Schema()
    {
        var count = await ExecuteAsAppRoleAsync($"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"Clients\"");

        Assert.Equal(1L, count);
    }

    [Fact]
    public async Task App_Role_Cannot_Create_A_Table_In_Its_Schema()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsAppRoleAsync($"CREATE TABLE {ControlPlaneDbContext.Schema}.\"Intruders\" (\"Id\" uuid)"));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    [Fact]
    public async Task App_Role_Cannot_Drop_A_Table_Of_Its_Schema()
    {
        var error = await Assert.ThrowsAsync<PostgresException>(
            () => ExecuteAsAppRoleAsync($"DROP TABLE {ControlPlaneDbContext.Schema}.\"Clients\""));

        Assert.Equal(PostgresErrorCodes.InsufficientPrivilege, error.SqlState);
    }

    // Fixed statements without input, so nothing is concatenated into SQL.
    private async Task<object?> ExecuteAsAppRoleAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(controlPlane.AppConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }
}
