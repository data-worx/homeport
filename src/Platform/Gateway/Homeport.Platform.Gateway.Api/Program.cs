// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Errors;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Platform.Gateway.Infrastructure.Signing;
using Homeport.Shared.ServiceDefaults;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api;

/// <summary>
/// The gateway: the one entry point of Homeport, which forwards
/// <c>/modules/{moduleId}/{path}</c> to the module and issues the tokens
/// Homeport's components trust. Every error it answers with is Problem
/// Details (ADR-0016).
/// </summary>
public sealed class Program
{
    private const string NoAdditionalKey = "none";

    // Not static, so the integration tests can name it as their entry point.
    private Program()
    {
    }

    /// <summary>Wires the gateway and runs it.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddGatewaySigning();
        builder.Services.AddControllers();
        builder.Services.AddAuthorization();
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<IConfigureOptions<ProblemDetailsOptions>, GatewayProblemDetails>();
        builder.Services.AddReverseProxy()
            .LoadFromMemory(StaticRoutes.Routes, StaticRoutes.Clusters)
            .AddTransforms<HomeportHeaders>()
            .AddTransforms<ModuleErrors>()
            .AddServiceDiscoveryDestinationResolver();

        var app = builder.Build();

        // In front of everything else: an exception, and an error status without
        // a body - an unknown module, a failed forward, a dropped module body -
        // become Problem Details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapDefaultEndpoints();
        app.UseAuthorization();
        app.MapControllers();

        // Mapped by YARP rather than served by a controller (dotnet-core.web):
        // the proxy endpoints are framework-mapped, like the health endpoints.
        app.MapReverseProxy();

        LogStart(app);
        app.Run();
    }

    // Once started, so the signing options have passed validation and the
    // addresses are bound. Key ids only: the keys are secrets.
    private static void LogStart(WebApplication app)
    {
        app.Lifetime.ApplicationStarted.Register(() =>
        {
            var signing = app.Services.GetRequiredService<IOptions<SigningOptions>>().Value;
            var additionalKeyId = string.IsNullOrWhiteSpace(signing.AdditionalKeyId) ? NoAdditionalKey : signing.AdditionalKeyId;

            app.Logger.LogInformation(
                "Gateway started on {Urls}: signing key {KeyId}, additional public key {AdditionalKeyId}, modules {ModuleIds}",
                app.Urls,
                signing.KeyId,
                additionalKeyId,
                StaticRoutes.Routes.Select(route => route.RouteId));
        });
    }
}
