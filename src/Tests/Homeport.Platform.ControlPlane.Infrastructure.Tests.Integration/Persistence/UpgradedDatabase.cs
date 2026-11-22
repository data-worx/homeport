// SPDX-License-Identifier: AGPL-3.0-or-later

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Homeport.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Homeport.Platform.ControlPlane.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// A throwaway PostgreSQL migrated to <see cref="ReleasedMigrations.V0_1_0"/>,
/// holding a plan, a tenant and a client written as v0.1.0 knew them, and
/// then migrated to the latest migration. Needs a running Docker.
/// </summary>
public sealed class UpgradedDatabase : IAsyncLifetime
{
    internal static readonly Guid PlanId = Guid.Parse("c3c3c3c3-0000-4000-8000-00000000000c");

    internal static readonly Guid TenantId = Guid.Parse("d4d4d4d4-0000-4000-8000-00000000000d");

    internal const string PlanName = "Standard";

    internal const int RequestsPerSecond = 10;

    internal const int Burst = 20;

    internal const string TenantName = "Sales";

    internal const string ClientId = "billing-service";

    private const string Database = "homeport";

    private const string OwnerRole = "control_plane_owner";

    private const string Password = "password";

    private const string InitScript = "init-homeport.sh";

    private const string InitDirectory = "/docker-entrypoint-initdb.d/";

    private const UnixFileModes Executable =
        UnixFileModes.UserRead | UnixFileModes.UserExecute
        | UnixFileModes.GroupRead | UnixFileModes.GroupExecute
        | UnixFileModes.OtherRead | UnixFileModes.OtherExecute;

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder(ContainerImages.Postgres)
        .WithDatabase(Database)
        .WithEnvironment("CONTROL_PLANE_OWNER_PASSWORD", Password)
        .WithEnvironment("CONTROL_PLANE_APP_PASSWORD", Password)
        // The script prepares every service's schema; this fixture uses only the control plane's.
        .WithEnvironment("METERING_OWNER_PASSWORD", Password)
        .WithEnvironment("METERING_APP_PASSWORD", Password)
        .WithEnvironment("METERING_ADMIN_PASSWORD", Password)
        .WithResourceMapping(FilePath.Of(Path.Combine(AppContext.BaseDirectory, InitScript)), DirectoryPath.Of(InitDirectory), fileMode: Executable)
        .Build();

    /// <summary>
    /// Runs <paramref name="sql"/> as the owner role and returns the columns
    /// of its single row, with <paramref name="parameters"/> as <c>$1</c>,
    /// <c>$2</c>, and so on.
    /// </summary>
    internal async Task<object[]> ReadRowAsync(string sql, params object[] parameters)
    {
        await using var connection = new NpgsqlConnection(OwnerConnectionString);
        await connection.OpenAsync();
        await using var command = Command(connection, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync(), "The row is gone.");
        var row = new object[reader.FieldCount];
        reader.GetValues(row);
        return row;
    }

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        await MigrateAsync(ReleasedMigrations.V0_1_0);
        await WriteV0_1_0DataAsync();
        await MigrateAsync(targetMigration: null);
    }

    public Task DisposeAsync() => postgres.DisposeAsync().AsTask();

    private string OwnerConnectionString => new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
    {
        Username = OwnerRole,
        Password = Password,
    }.ConnectionString;

    private static NpgsqlCommand Command(NpgsqlConnection connection, string sql, object[] parameters)
    {
        var command = new NpgsqlCommand(sql, connection);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = parameter });
        }

        return command;
    }

    // The migrate command's way: the owner role, the service's own persistence wiring.
    // The host is never started: only its services are used, so there is nothing to stop.
    private async Task MigrateAsync(string? targetMigration)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{ControlPlanePersistenceExtensions.AppConnectionName}"] = OwnerConnectionString,
        });
        builder.AddControlPlanePersistence(ControlPlanePersistenceExtensions.AppConnectionName);
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>().Database.MigrateAsync(targetMigration);
    }

    // The tables as the last migration of v0.1.0 created them, never as the model is now.
    private async Task WriteV0_1_0DataAsync()
    {
        await using var connection = new NpgsqlConnection(OwnerConnectionString);
        await connection.OpenAsync();
        await using var batch = new NpgsqlBatch(connection)
        {
            BatchCommands =
            {
                BatchCommand(
                    $"""INSERT INTO {ControlPlaneDbContext.Schema}."Plans" ("Id", "Name", "RequestsPerSecond", "Burst") VALUES ($1, $2, $3, $4)""",
                    PlanId, PlanName, RequestsPerSecond, Burst),
                BatchCommand(
                    $"""INSERT INTO {ControlPlaneDbContext.Schema}."Tenants" ("Id", "Name") VALUES ($1, $2)""",
                    TenantId, TenantName),
                BatchCommand(
                    $"""INSERT INTO {ControlPlaneDbContext.Schema}."Clients" ("ClientId", "TenantId", "PlanId") VALUES ($1, $2, $3)""",
                    ClientId, TenantId, PlanId),
            },
        };

        await batch.ExecuteNonQueryAsync();
    }

    private static NpgsqlBatchCommand BatchCommand(string sql, params object[] parameters)
    {
        var command = new NpgsqlBatchCommand(sql);
        foreach (var parameter in parameters)
        {
            command.Parameters.Add(new NpgsqlParameter { Value = parameter });
        }

        return command;
    }
}
