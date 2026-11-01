// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Homeport.Platform.ControlPlane.Persistence;

/// <summary>Wires the control plane's database.</summary>
internal static class ControlPlanePersistenceExtensions
{
    /// <summary>The connection the service runs on, as the app role: no DDL rights.</summary>
    internal const string AppConnectionName = "control-plane";

    /// <summary>
    /// Adds <see cref="ControlPlaneDbContext"/> on the connection string
    /// <paramref name="connectionName"/>, with health check, tracing and retries.
    /// </summary>
    internal static IHostApplicationBuilder AddControlPlanePersistence(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.AddNpgsqlDbContext<ControlPlaneDbContext>(
            connectionName,
            configureDbContextOptions: options => ConfigureNpgsql(options));

        return builder;
    }

    /// <summary>Keeps the migration history in the control plane's own schema.</summary>
    internal static DbContextOptionsBuilder ConfigureNpgsql(DbContextOptionsBuilder options)
    {
        return options.UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, ControlPlaneDbContext.Schema));
    }
}
