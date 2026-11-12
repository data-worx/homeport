// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Homeport.Platform.ControlPlane.Infrastructure.Seeding;
using Homeport.Shared.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.ControlPlane.Api;

/// <summary>
/// <c>migrate</c>: applies the pending migrations as the owner role, seeds
/// the tenant, plan and client from <see cref="SeedOptions"/>, and exits. The
/// control plane starts only after it completed.
/// </summary>
internal static class MigrateCommand
{
    /// <summary>The first argument that runs this command instead of the service.</summary>
    internal const string Name = "migrate";

    /// <summary>The connection it runs on, as the owner role: the one with DDL rights.</summary>
    internal const string OwnerConnectionName = "control-plane-owner";

    /// <summary>Runs the command; returns the process exit code.</summary>
    /// <param name="args">The arguments after <see cref="Name"/>, as configuration.</param>
    internal static async Task<int> RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();
        builder.AddControlPlanePersistence(OwnerConnectionName);
        builder.AddControlPlaneSeeding();

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MigrateCommand));
        var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

        await host.StartAsync(stopping);
        try
        {
            var seed = host.Services.GetRequiredService<IOptions<SeedOptions>>().Value;

            await using var scope = host.Services.CreateAsyncScope();
            var database = scope.ServiceProvider.GetRequiredService<ControlPlaneDbContext>();
            // Through the execution strategy like everything after it: the first
            // connection may come while PostgreSQL still initialises, and is
            // retried rather than failing the step.
            var pending = (await database.Database.CreateExecutionStrategy()
                .ExecuteAsync(database.Database.GetPendingMigrationsAsync, stopping)).ToList();
            await database.Database.MigrateAsync(stopping);
            await scope.ServiceProvider.GetRequiredService<ControlPlaneSeeder>().SeedAsync(stopping);

            logger.LogInformation(
                "Control plane migrated: applied {Migrations}, seeded tenant {TenantId}, plan {PlanId} and client {ClientId}",
                pending,
                seed.TenantId,
                seed.PlanId,
                seed.ClientId);
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Migrating the control plane failed");
            return 1;
        }
        finally
        {
            // Flushes the telemetry of the run before the process exits.
            await host.StopAsync(CancellationToken.None);
        }
    }
}
