// SPDX-License-Identifier: AGPL-3.0-or-later

using DotNet.Testcontainers.Configurations;
using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Homeport.Testing.Containers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Homeport.Platform.ControlPlane.Infrastructure.Tests.Integration;

/// <summary>
/// A throwaway PostgreSQL prepared by <c>deploy/postgres/init/init-homeport.sh</c>
/// and migrated as the owner role, without a seed. Needs a running Docker.
/// </summary>
public sealed class ControlPlaneDatabase : IAsyncLifetime
{
    /// <summary>The collection that shares one database.</summary>
    internal const string Collection = "control plane database";

    private const string Database = "homeport";

    private const string OwnerRole = "control_plane_owner";

    private const string AppRole = "control_plane_app";

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
        .WithEnvironment("CONTROL_PLANE_OWNER_PASSWORD", OwnerPassword)
        .WithEnvironment("CONTROL_PLANE_APP_PASSWORD", AppPassword)
        // The script prepares every service's schema; this fixture uses only the control plane's.
        .WithEnvironment("METERING_OWNER_PASSWORD", OwnerPassword)
        .WithEnvironment("METERING_APP_PASSWORD", AppPassword)
        .WithResourceMapping(new FileInfo(Path.Combine(AppContext.BaseDirectory, InitScript)), InitDirectory, fileMode: Executable)
        .Build();

    private IHost? app;

    private IHost App => app ?? throw new InvalidOperationException("The database has not been prepared.");

    /// <summary>A scope on the app role's connection, which the service runs on.</summary>
    internal AsyncServiceScope CreateScope() => App.Services.CreateAsyncScope();

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();

        using (var owner = BuildHost(ConnectionString(OwnerRole, OwnerPassword)))
        {
            await using var scope = owner.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>().Database.MigrateAsync();
        }

        app = BuildHost(ConnectionString(AppRole, AppPassword));
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
            [$"ConnectionStrings:{ControlPlanePersistenceExtensions.AppConnectionName}"] = connectionString,
        });
        builder.AddControlPlanePersistence(ControlPlanePersistenceExtensions.AppConnectionName);
        return builder.Build();
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
