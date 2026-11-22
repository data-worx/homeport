// SPDX-License-Identifier: AGPL-3.0-or-later

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using Homeport.Platform.Metering.Infrastructure.Persistence;
using Homeport.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration.Persistence;

/// <summary>
/// A throwaway PostgreSQL migrated to <see cref="ReleasedMigrations.V0_1_0"/>,
/// holding an event and its hourly and daily aggregates written as v0.1.0
/// knew them, and then migrated to the latest migration. Needs a running Docker.
/// </summary>
public sealed class UpgradedDatabase : IAsyncLifetime
{
    internal static readonly Guid EventId = Guid.Parse("e5e5e5e5-0000-4000-8000-00000000000e");

    internal static readonly Guid TenantId = Guid.Parse("a1a1a1a1-0000-4000-8000-00000000000a");

    internal static readonly DateTime OccurredAt = new(2026, 11, 4, 13, 30, 0, DateTimeKind.Utc);

    internal static readonly DateTime Hour = new(2026, 11, 4, 13, 0, 0, DateTimeKind.Utc);

    internal static readonly DateOnly Day = new(2026, 11, 4);

    internal const string ClientId = "billing-service";

    internal const string ModuleId = "echo";

    internal const string Route = "/v1/echo";

    internal const string Meter = "requests";

    internal const int Quantity = 1;

    internal const int StatusCode = 200;

    internal const long DurationMs = 5;

    internal const string TraceId = "0af7651916cd43dd8448eb211c80319c";

    private const string Database = "homeport";

    private const string OwnerRole = "metering_owner";

    private const string Password = "password";

    private const string InitScript = "init-homeport.sh";

    private const string InitDirectory = "/docker-entrypoint-initdb.d/";

    private const UnixFileModes Executable =
        UnixFileModes.UserRead | UnixFileModes.UserExecute
        | UnixFileModes.GroupRead | UnixFileModes.GroupExecute
        | UnixFileModes.OtherRead | UnixFileModes.OtherExecute;

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder(ContainerImages.Postgres)
        .WithDatabase(Database)
        .WithEnvironment("METERING_OWNER_PASSWORD", Password)
        .WithEnvironment("METERING_APP_PASSWORD", Password)
        // The admin role v0.2.0 adds exists before the upgrade, as upgrading.md has it created.
        .WithEnvironment("METERING_ADMIN_PASSWORD", Password)
        // The script prepares every service's schema; this fixture uses only metering's.
        .WithEnvironment("CONTROL_PLANE_OWNER_PASSWORD", Password)
        .WithEnvironment("CONTROL_PLANE_APP_PASSWORD", Password)
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
    // The owner role owns the tables, so row-level security, which is not forced, admits it.
    // The host is never started: only its services are used, so there is nothing to stop.
    private async Task MigrateAsync(string? targetMigration)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{MeteringPersistenceExtensions.AppConnectionName}"] = OwnerConnectionString,
        });
        builder.AddMeteringPersistence(MeteringPersistenceExtensions.AppConnectionName);
        using var host = builder.Build();
        await using var scope = host.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<MeteringDbContext>().Database.MigrateAsync(targetMigration);
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
                    $"""
                    INSERT INTO {MeteringDbContext.Schema}."UsageEvents"
                        ("EventId", "OccurredAt", "TenantId", "ClientId", "ModuleId", "Route", "Meter", "Quantity", "StatusCode", "DurationMs", "TraceId")
                    VALUES ($1, $2, $3, $4, $5, $6, $7, $8, $9, $10, $11)
                    """,
                    EventId, OccurredAt, TenantId, ClientId, ModuleId, Route, Meter, Quantity, StatusCode, DurationMs, TraceId),
                BatchCommand(
                    $"""INSERT INTO {MeteringDbContext.Schema}."UsageHourly" ("TenantId", "ModuleId", "Meter", "Hour", "Quantity") VALUES ($1, $2, $3, $4, $5)""",
                    TenantId, ModuleId, Meter, Hour, (long)Quantity),
                BatchCommand(
                    $"""INSERT INTO {MeteringDbContext.Schema}."UsageDaily" ("TenantId", "ModuleId", "Meter", "Day", "Quantity") VALUES ($1, $2, $3, $4, $5)""",
                    TenantId, ModuleId, Meter, Day, (long)Quantity),
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
