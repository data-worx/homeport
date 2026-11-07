// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Api.Authentication;
using Homeport.Platform.Metering.Api.Errors;
using Homeport.Platform.Metering.Api.Export;
using Homeport.Platform.Metering.Api.Messaging;
using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Infrastructure.Persistence;
using Homeport.Shared.Authentication;
using Homeport.Shared.Internal.Platform.Metering;
using Homeport.Shared.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Metering.Api;

/// <summary>
/// Metering: consumes the gateway's usage events and aggregates them per
/// tenant, module, meter, hour and day, each event exactly once (ADR-0007),
/// and exports a tenant's own usage to the gateway's forwarded requests.
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
        builder.Services.AddScoped<UsageRecording>();
        builder.Services.AddScoped<UsageExport>();
        builder.AddMeteringMessaging();

        // Only the gateway's tokens for metering; the export also needs the
        // tenant and scope of a caller token (ADR-0019).
        builder.Services.AddGatewayTokenAuthentication(MeteringService.Audience);
        builder.Services.AddSingleton<IAuthorizationHandler, UsageReaderHandler>();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy(UsageReaderRequirement.Policy, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new UsageReaderRequirement()));
        builder.Services.AddControllers(options => options.OutputFormatters.Add(new UsageCsvOutputFormatter()));
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<IConfigureOptions<ProblemDetailsOptions>, MeteringProblemDetails>();

        var app = builder.Build();

        // In front of everything else: an exception, and an error status without
        // a body, become Problem Details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapDefaultEndpoints();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Lifetime.ApplicationStarted.Register(() => app.Logger.LogInformation("Metering started on {Urls}", app.Urls));
        await app.RunAsync();
    }
}
