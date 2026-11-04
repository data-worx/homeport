// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Infrastructure.Persistence;
using Homeport.Shared.ServiceDefaults;

namespace Homeport.Platform.Metering.Api;

/// <summary>
/// Metering: consumes the gateway's usage events and aggregates them per
/// tenant, module, meter, hour and day, each event exactly once (ADR-0007).
/// </summary>
public sealed class Program
{
    // Not static, so the integration tests can name it as their entry point.
    private Program()
    {
    }

    /// <summary>
    /// Runs metering, or with <c>migrate</c> as the first argument the
    /// <see cref="MigrateCommand"/>.
    /// </summary>
    /// <returns>The process exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        if (args is [MigrateCommand.Name, .. var rest])
        {
            return await MigrateCommand.RunAsync(rest);
        }

        await RunAsync(args);
        return 0;
    }

    private static async Task RunAsync(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.AddMeteringPersistence(MeteringPersistenceExtensions.AppConnectionName);
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        // In front of everything else: an exception, and an error status without
        // a body, become Problem Details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapDefaultEndpoints();

        app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("Metering started on {Urls}", app.Urls));
        await app.RunAsync();
    }
}
