// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Authentication;
using Homeport.Platform.Gateway.Api.Errors;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Platform.Gateway.Infrastructure.ControlPlane;
using Homeport.Platform.Gateway.Infrastructure.Registry;
using Homeport.Platform.Gateway.Infrastructure.Signing;
using Homeport.Shared.ServiceDefaults;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api;

/// <summary>
/// The gateway: the one entry point of Homeport, which forwards
/// <c>/modules/{moduleId}/{path}</c> to the module on the routes the registry
/// knows for clients the control plane knows, and issues the tokens
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
        builder.Services.AddRegistryClient();
        builder.Services.AddControlPlaneClient();
        builder.Services.AddClientAuthentication();
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.Configure<ExceptionHandlerOptions>(options =>
        {
            options.StatusCodeSelector = exception => exception is ControlPlaneUnavailableException
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status500InternalServerError;

            // Logged as the degraded path it is, where it is answered.
            options.SuppressDiagnosticsCallback = context => context.Exception is ControlPlaneUnavailableException;
        });
        builder.Services.AddSingleton<IConfigureOptions<ProblemDetailsOptions>, GatewayProblemDetails>();
        builder.Services.AddReverseProxy()
            .LoadFromMemory([], [])
            .AddTransforms<HomeportHeaders>()
            .AddTransforms<ModuleErrors>();
        builder.Services.AddHostedService<RouteTablePoller>();

        var app = builder.Build();

        // In front of everything else: an exception, and an error status without
        // a body - an unknown module, a failed forward, a dropped module body -
        // become Problem Details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapDefaultEndpoints();
        app.UseAuthentication();
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
            var clients = app.Services.GetRequiredService<IOptions<ClientAuthenticationOptions>>().Value;
            var controlPlane = app.Services.GetRequiredService<IOptions<ControlPlaneOptions>>().Value;

            app.Logger.LogInformation(
                "Gateway started on {Urls}: signing key {KeyId}, additional public key {AdditionalKeyId}, routes from {RegistryUrl}, client tokens from {Authority} for {Audience} with the client id in {ClientIdClaim}, clients from {ControlPlaneUrl} kept for {ClientCacheLifetime}",
                app.Urls,
                signing.KeyId,
                additionalKeyId,
                app.Services.GetRequiredService<IOptions<RegistryOptions>>().Value.Url,
                clients.Authority,
                clients.Audience,
                clients.ClientIdClaim,
                controlPlane.Url,
                controlPlane.ClientCacheLifetime);
        });
    }
}
