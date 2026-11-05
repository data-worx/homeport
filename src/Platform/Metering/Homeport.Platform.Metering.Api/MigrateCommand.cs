// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Infrastructure.Persistence;
using Homeport.Shared.ServiceDefaults;

namespace Homeport.Platform.Metering.Api;

/// <summary>
/// <c>migrate</c>: applies the pending migrations as the owner role and
/// exits. Metering starts only after it completed.
/// </summary>
internal static class MigrateCommand
{
    /// <summary>The first argument that runs this command instead of the service.</summary>
    internal const string Name = "migrate";

    /// <summary>The connection it runs on, as the owner role: the one with DDL rights.</summary>
    internal const string OwnerConnectionName = "metering-owner";

    /// <summary>Runs the command; returns the process exit code.</summary>
    /// <param name="args">The arguments after <see cref="Name"/>, as configuration.</param>
    internal static async Task<int> RunAsync(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.AddServiceDefaults();
        builder.AddMeteringPersistence(OwnerConnectionName);

        using var host = builder.Build();
        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(MigrateCommand));
        var stopping = host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;

        await host.StartAsync(stopping);
        try
        {
            await using var scope = host.Services.CreateAsyncScope();
            var applied = await MeteringMigrations.ApplyAsync(scope.ServiceProvider, stopping);

            logger.LogInformation("Metering migrated: applied {Migrations}", applied);
            return 0;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Migrating metering failed");
            return 1;
        }
        finally
        {
            // Flushes the telemetry of the run before the process exits.
            await host.StopAsync(CancellationToken.None);
        }
    }
}
