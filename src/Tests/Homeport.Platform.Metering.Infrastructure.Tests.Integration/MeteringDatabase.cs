// SPDX-License-Identifier: AGPL-3.0-or-later

using DotNet.Testcontainers.Configurations;
using Homeport.Platform.Metering.Domain.Usage;
using Homeport.Platform.Metering.Infrastructure.Persistence;
using Homeport.Testing.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Homeport.Platform.Metering.Infrastructure.Tests.Integration;

/// <summary>
/// A throwaway PostgreSQL prepared by <c>deploy/postgres/init/init-homeport.sh</c>
/// and migrated as the owner role, holding one event and its aggregates for
/// each of <see cref="TenantA"/> and <see cref="TenantB"/>. Needs a running Docker.
/// </summary>
public sealed class MeteringDatabase : IAsyncLifetime
{
    /// <summary>The collection that shares one database.</summary>
    internal const string Collection = "metering database";

    internal static readonly Guid TenantA = Guid.Parse("a1a1a1a1-0000-4000-8000-00000000000a");

    internal static readonly Guid TenantB = Guid.Parse("b2b2b2b2-0000-4000-8000-00000000000b");

    private const string Database = "homeport";

    private const string OwnerRole = "metering_owner";

    private const string AppRole = "metering_app";

    private const string OwnerPassword = "owner-password";

    private const string AppPassword = "app-password";

    private const string InitScript = "init-homeport.sh";

    private const string InitDirectory = "/docker-entrypoint-initdb.d/";

    private const UnixFileModes Executable =
        UnixFileModes.UserRead | UnixFileModes.UserExecute
        | UnixFileModes.GroupRead | UnixFileModes.GroupExecute
        | UnixFileModes.OtherRead | UnixFileModes.OtherExecute;

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder(ContainerImages.Postgres)
        .WithDatabase(Database)
        .WithEnvironment("METERING_OWNER_PASSWORD", OwnerPassword)
        .WithEnvironment("METERING_APP_PASSWORD", AppPassword)
        // The script prepares every service's schema; this fixture uses only metering's.
        .WithEnvironment("CONTROL_PLANE_OWNER_PASSWORD", OwnerPassword)
        .WithEnvironment("CONTROL_PLANE_APP_PASSWORD", AppPassword)
        .WithResourceMapping(new FileInfo(Path.Combine(AppContext.BaseDirectory, InitScript)), InitDirectory, fileMode: Executable)
        .Build();

    private IHost? app;

    /// <summary>The app role's connection string, which the service runs on.</summary>
    internal string AppConnectionString => ConnectionString(AppRole, AppPassword);

    private IHost App => app ?? throw new InvalidOperationException("The database has not been prepared.");

    /// <summary>A scope on the app role's connection that works for <paramref name="tenantId"/>, or for no tenant.</summary>
    internal AsyncServiceScope CreateScope(Guid? tenantId)
    {
        var scope = App.Services.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<CurrentTenant>().Id = tenantId;
        return scope;
    }

    /// <summary>The event every tenant of this fixture holds, for <paramref name="tenantId"/>.</summary>
    internal static UsageEvent EventOf(Guid tenantId)
    {
        return new UsageEvent(
            Guid.NewGuid(),
            DateTimeOffset.Parse("2026-11-04T13:30:00Z"),
            tenantId,
            "client",
            "echo",
            "/v1/echo",
            "requests",
            1,
            200,
            5,
            "0af7651916cd43dd8448eb211c80319c");
    }

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        using (var owner = BuildHost(ConnectionString(OwnerRole, OwnerPassword)))
        {
            await using var scope = owner.Services.CreateAsyncScope();
            await MeteringMigrations.ApplyAsync(scope.ServiceProvider, CancellationToken.None);
        }

        app = BuildHost(AppConnectionString);
        await SeedAsync(TenantA);
        await SeedAsync(TenantB);
    }

    public async Task DisposeAsync()
    {
        app?.Dispose();
        await postgres.DisposeAsync();
    }

    // Never started: the tests use only its services, so there is nothing to stop.
    private static IHost BuildHost(string connectionString)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"ConnectionStrings:{MeteringPersistenceExtensions.AppConnectionName}"] = connectionString,
        });
        builder.AddMeteringPersistence(MeteringPersistenceExtensions.AppConnectionName);
        return builder.Build();
    }

    // As the app role, for the tenant itself: the policies admit a tenant's own rows.
    private async Task SeedAsync(Guid tenantId)
    {
        await using var scope = CreateScope(tenantId);
        var database = scope.ServiceProvider.GetRequiredService<MeteringDbContext>();
        var usage = EventOf(tenantId);

        database.UsageEvents.Add(usage);
        database.UsageHourly.Add(new HourlyUsage(tenantId, usage.ModuleId, usage.Meter, UsageBuckets.HourOf(usage.OccurredAt), usage.Quantity));
        database.UsageDaily.Add(new DailyUsage(tenantId, usage.ModuleId, usage.Meter, UsageBuckets.DayOf(usage.OccurredAt), usage.Quantity));
        await database.SaveChangesAsync();
    }

    private string ConnectionString(string role, string password)
    {
        return new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
        {
            Username = role,
            Password = password,
        }.ConnectionString;
    }
}
