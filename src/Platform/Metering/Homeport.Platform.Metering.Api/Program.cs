// SPDX-License-Identifier: AGPL-3.0-or-later

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

    /// <summary>Wires metering and runs it.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddProblemDetails();

        var app = builder.Build();

        // In front of everything else: an exception, and an error status without
        // a body, become Problem Details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapDefaultEndpoints();

        app.Run();
    }
}
