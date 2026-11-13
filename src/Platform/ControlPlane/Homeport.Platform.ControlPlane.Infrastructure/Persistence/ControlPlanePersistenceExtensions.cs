// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Application;
using Homeport.Platform.ControlPlane.Application.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence;

/// <summary>Wires the control plane's database.</summary>
public static class ControlPlanePersistenceExtensions
{
    /// <summary>The connection the service runs on, as the app role: no DDL rights.</summary>
    public const string AppConnectionName = "control-plane";

    /// <summary>
    /// Adds <see cref="ControlPlaneDbContext"/> on the connection string
    /// <paramref name="connectionName"/>, with health check, tracing and
    /// retries, and the <see cref="IClientLookup"/> and <see cref="ITenantStore"/> on it.
    /// </summary>
    public static IHostApplicationBuilder AddControlPlanePersistence(this IHostApplicationBuilder builder, string connectionName)
    {
        builder.AddNpgsqlDbContext<ControlPlaneDbContext>(
            connectionName,
            configureDbContextOptions: options => ConfigureNpgsql(options));
        builder.Services.AddScoped<IClientLookup, ClientLookup>();
        builder.Services.AddScoped<ITenantStore, TenantStore>();

        return builder;
    }

    /// <summary>Keeps the migration history in the control plane's own schema.</summary>
    internal static DbContextOptionsBuilder ConfigureNpgsql(DbContextOptionsBuilder options)
    {
        return options.UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, ControlPlaneDbContext.Schema));
    }
}
