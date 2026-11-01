// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Errors;
using Homeport.Platform.ControlPlane.Persistence;
using Homeport.Shared.Authentication;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Homeport.Shared.ServiceDefaults;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.ControlPlane;

/// <summary>
/// The control plane: it knows the tenants, their plans and their clients,
/// and tells the gateway which tenant and plan a client belongs to.
/// </summary>
public sealed class Program
{
    // Not static, so the integration tests can name it as their entry point.
    private Program()
    {
    }

    /// <summary>
    /// Runs the control plane, or with <c>migrate</c> as the first argument
    /// the <see cref="MigrateCommand"/>.
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
        builder.AddControlPlanePersistence(ControlPlanePersistenceExtensions.AppConnectionName);

        builder.Services.AddGatewayTokenAuthentication(ControlPlaneService.Audience);
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<IConfigureOptions<ProblemDetailsOptions>, ControlPlaneProblemDetails>();

        var app = builder.Build();

        // In front of everything else: an exception, and an error status without
        // a body - a failed authentication above all - become Problem Details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapDefaultEndpoints();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("Control plane started on {Urls}", app.Urls));
        await app.RunAsync();
    }
}
