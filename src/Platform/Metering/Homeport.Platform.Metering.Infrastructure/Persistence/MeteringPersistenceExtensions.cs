// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Application;
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
    /// The connection the operator's reads run on, as the admin role: it reads
    /// every tenant's aggregates and adds audit entries, nothing else (ADR-0020).
    /// </summary>
    public const string AdminConnectionName = "metering-admin";

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
        builder.Services.AddScoped<IUsageStore, UsageStore>();
        builder.Services.AddScoped<IUsageReader, UsageReader>();

        return builder;
    }

    /// <summary>
    /// Adds the operator's reads of every tenant's usage on the connection
    /// string <see cref="AdminConnectionName"/>, with retries. Needs
    /// <see cref="AddMeteringPersistence"/> first.
    /// </summary>
    /// <remarks>
    /// The context on that connection is keyed, so nothing but
    /// <see cref="AdminUsageReader"/> reaches it, and carries no tenant
    /// session setting: the admin role reads through its own policy.
    /// </remarks>
    public static IHostApplicationBuilder AddMeteringAdministration(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString(AdminConnectionName)
            ?? throw new InvalidOperationException($"The connection string '{AdminConnectionName}' is missing.");

        var options = ConfigureNpgsql(new DbContextOptionsBuilder<MeteringDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure()));
        var adminOptions = (DbContextOptions<MeteringDbContext>)options.Options;

        builder.Services.AddKeyedScoped(AdminConnectionName, (_, _) => new MeteringDbContext(adminOptions, new CurrentTenant()));
        builder.Services.AddScoped<IAdminUsageReader, AdminUsageReader>();

        return builder;
    }

    /// <summary>Keeps the migration history in metering's own schema.</summary>
    internal static DbContextOptionsBuilder ConfigureNpgsql(DbContextOptionsBuilder options)
    {
        return options.UseNpgsql(npgsql => npgsql.MigrationsHistoryTable(HistoryRepository.DefaultTableName, MeteringDbContext.Schema));
    }
}
