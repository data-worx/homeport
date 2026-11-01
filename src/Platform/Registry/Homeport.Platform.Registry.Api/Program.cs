// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Api.Authentication;
using Homeport.Platform.Registry.Api.Errors;
using Homeport.Platform.Registry.Application;
using Homeport.Platform.Registry.Infrastructure;
using Homeport.Platform.Registry.Infrastructure.Keys;
using Homeport.Shared.Authentication;
using Homeport.Shared.Internal.Platform.Registry;
using Homeport.Shared.ServiceDefaults;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Registry.Api;

/// <summary>
/// The registry: module instances register here with their manifests and send
/// heartbeats, and the gateway reads the routes of the live ones (ADR-0004).
/// </summary>
public sealed class Program
{
    // Not static, so the integration tests can name it as their entry point.
    private Program()
    {
    }

    /// <summary>Wires the registry and runs it.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.AddRegistryInfrastructure();
        builder.Services.AddSingleton<Registrations>();

        // The gateway's service tokens are the default, for the routes; a
        // module instance authenticates with its registration key instead.
        builder.Services.AddGatewayTokenAuthentication(RegistryService.Audience)
            .AddScheme<AuthenticationSchemeOptions, RegistrationKeyHandler>(RegistrationKeyHandler.SchemeName, configureOptions: null);
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<IConfigureOptions<ProblemDetailsOptions>, RegistryProblemDetails>();

        var app = builder.Build();

        // In front of everything else: an exception, and an error status without
        // a body - a failed authentication above all - become Problem Details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapDefaultEndpoints();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        LogStart(app);
        app.Run();
    }

    // Once started, so the registration keys have passed validation. Module
    // ids and key counts only: the keys are secrets.
    private static void LogStart(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var keys = app.Services.GetRequiredService<IOptions<RegistrationKeyOptions>>().Value.RegistrationKeys;

            app.Logger.LogInformation(
                "Registry started on {Urls}: registration keys for modules {ModuleIds}, instances live for {InstanceLifetime}",
                app.Urls,
                keys.Select(module => $"{module.Key} ({module.Value.Length})"),
                Registrations.InstanceLifetime);
        });
    }
}
