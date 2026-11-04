// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>Wires metering's database.</summary>
public static class MeteringPersistenceExtensions
{
    /// <summary>The connection the service runs on, as the app role: no DDL rights.</summary>
    public const string AppConnectionName = "metering";

    /// <summary>
    /// Adds the metering database on the connection string
    /// <paramref name="connectionName"/>, with health check, tracing and retries.
    /// </summary>
    /// <remarks>
    /// Not pooled, unlike Aspire's default: each scope carries the tenant it
    /// works for into its context.
    /// </remarks>
    public static IHostApplicationBuilder AddMeteringPersistence(this IHostApplicationBuilder builder, string connectionName)
    {
        var connectionString = builder.Configuration.GetConnectionString(connectionName)
            ?? throw new InvalidOperationException($"The connection string '{connectionName}' is missing.");

        builder.Services.AddScoped<CurrentTenant>();
        builder.Services.AddScoped<TenantSessionInterceptor>();
        builder.Services.AddDbContext<MeteringDbContext>((services, options) => ConfigureNpgsql(options.UseNpgsql(connectionString))
            .AddInterceptors(services.GetRequiredService<TenantSessionInterceptor>()));
        builder.EnrichNpgsqlDbContext<MeteringDbContext>();

        return builder;
    }

    /// <summary>Keeps the migration history in metering's own schema.</summary>
    internal static DbContextOptionsBuilder ConfigureNpgsql(DbContextOptionsBuilder options)
    {
        return options.UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, MeteringDbContext.Schema));
    }
}
