// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Admin;
using Homeport.Platform.Gateway.Api.ApiDocs;
using Homeport.Platform.Gateway.Api.Authentication;
using Homeport.Platform.Gateway.Api.Errors;
using Homeport.Platform.Gateway.Api.Proxies;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Platform.Gateway.Api.Telemetry;
using Homeport.Platform.Gateway.Api.Throttling;
using Homeport.Platform.Gateway.Api.Usage;
using Homeport.Platform.Gateway.Infrastructure.ControlPlane;
using Homeport.Platform.Gateway.Infrastructure.Registry;
using Homeport.Platform.Gateway.Infrastructure.Signing;
using Homeport.Platform.Gateway.Infrastructure.Throttling;
using Homeport.Platform.Gateway.Infrastructure.Usage;
using Homeport.Shared.ServiceDefaults;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace Homeport.Platform.Gateway.Api;

/// <summary>
/// The gateway: the one entry point of Homeport, which forwards
/// <c>/modules/{moduleId}/{path}</c> to the module on the routes the registry
/// knows, and <c>/platform/{path}</c> to the platform service behind it, for
/// clients the control plane knows, and the admin API on a port of its own
/// for the operator (ADR-0020), and issues the tokens
/// Homeport's components trust. Every error it answers with is Problem
/// Details (ADR-0016).
/// </summary>
public sealed class Program
{
    private const string None = "none";

    // Not static, so the integration tests can name it as their entry point.
    private Program()
    {
    }

    /// <summary>Wires the gateway and runs it.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddTrustedProxies();
        builder.Services.AddGatewaySigning();
        builder.Services.AddRegistryClient();
        builder.Services.AddControlPlaneClient();
        builder.Services.AddClientAuthentication();
        builder.Services.AddAdminApi();
        builder.AddUsagePublishing();
        builder.AddThrottling();
        builder.Services.AddControllers();
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<IProblemDetailsWriter, AnyAcceptProblemDetailsWriter>();
        builder.Services.Configure<ExceptionHandlerOptions>(options =>
        {
            options.StatusCodeSelector = exception => exception is ControlPlaneUnavailableException
                ? StatusCodes.Status503ServiceUnavailable
                : StatusCodes.Status500InternalServerError;

            // Logged as the degraded path it is, where it is answered.
            options.SuppressDiagnosticsCallback = context => context.Exception is ControlPlaneUnavailableException;
        });
        builder.Services.AddSingleton<IConfigureOptions<ProblemDetailsOptions>, GatewayProblemDetails>();
        builder.Services.AddOptions<PlatformRoutesOptions>().BindConfiguration(PlatformRoutesOptions.SectionName);
        builder.Services.AddSingleton<PlatformRoutes>();

        // What LoadFromMemory registers, but starting with the platform routes,
        // so they are served before the registry has been read.
        builder.Services.AddSingleton(services =>
        {
            var (routes, clusters) = services.GetRequiredService<PlatformRoutes>().Build();
            return new InMemoryConfigProvider(routes, clusters);
        });
        builder.Services.AddSingleton<IProxyConfigProvider>(services => services.GetRequiredService<InMemoryConfigProvider>());
        builder.Services.AddReverseProxy()
            .AddTransforms<HomeportHeaders>()
            .AddTransforms<ModuleTokens>()
            .AddTransforms<UsageReports>()
            .AddTransforms<ModuleErrors>();
        builder.Services.AddHostedService<RouteTablePoller>();
        builder.Services.AddSingleton<RequestMetrics>();
        builder.Services.AddApiDocs();

        var app = builder.Build();

        // First, so everything after it sees the caller's address and scheme
        // as a trusted proxy reports them, and nobody else can.
        app.UseForwardedHeaders();

        // In front of everything else: an exception, and an error status without
        // a body - an unknown module, a failed forward, a dropped module body -
        // become Problem Details.
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        // Before any token is looked at: the admin API exists on the admin
        // port only, and the admin port serves nothing else (ADR-0020).
        app.UseMiddleware<AdminPortBoundary>();

        app.MapDefaultEndpoints();
        app.UseAuthentication();
        app.UseMiddleware<ModuleRequestChallenge>();
        app.UseAuthorization();
        app.MapControllers();

        // Off, the view is not mapped at all and answers 404 like any unknown path.
        var docs = app.Services.GetRequiredService<IOptions<ApiDocsOptions>>().Value;
        if (docs.Enabled)
        {
            app.MapApiDocs();
        }

        // Mapped by YARP rather than served by a controller (dotnet-core.web):
        // the proxy endpoints are framework-mapped, like the health endpoints.
        // Its telemetry comes first, so a throttled request is traced and
        // counted too. A request over its tenant's rate limit is rejected
        // next, so it is not metered; usage is recorded around YARP's own
        // steps, which a custom pipeline has to name.
        app.MapReverseProxy(proxy =>
        {
            proxy.UseMiddleware<ModuleRequestTelemetry>();
            proxy.UseMiddleware<RequestThrottling>();
            proxy.UseMiddleware<UsageEvents>();
            proxy.UseSessionAffinity();
            proxy.UseLoadBalancing();
            proxy.UsePassiveHealthChecks();
        });

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
            var additionalKeyId = string.IsNullOrWhiteSpace(signing.AdditionalKeyId) ? None : signing.AdditionalKeyId;
            var clients = app.Services.GetRequiredService<IOptions<ClientAuthenticationOptions>>().Value;
            var controlPlane = app.Services.GetRequiredService<IOptions<ControlPlaneOptions>>().Value;
            var moduleTokens = app.Services.GetRequiredService<IOptions<ModuleTokenOptions>>().Value;
            var usage = app.Services.GetRequiredService<IOptions<UsageOptions>>().Value;
            var proxies = app.Services.GetRequiredService<IOptions<TrustedProxiesOptions>>().Value;
            var docs = app.Services.GetRequiredService<IOptions<ApiDocsOptions>>().Value;
            var platform = app.Services.GetRequiredService<IOptions<PlatformRoutesOptions>>().Value;
            var admin = app.Services.GetRequiredService<IOptions<AdminApiOptions>>().Value;

            app.Logger.LogInformation(
                "Gateway started on {Urls}: signing key {KeyId}, additional public key {AdditionalKeyId}, routes from {RegistryUrl}, usage from {MeteringUrl}, client tokens from {Authority} for {Audience} with the client id in {ClientIdClaim} and the scopes in {ScopeClaim}, clients from {ControlPlaneUrl} kept for {ClientCacheLifetime}, module tokens valid for {ModuleTokenLifetime}, usage events retried after {UsageRetryDelays}, forwarded headers from proxies {TrustedProxies} and networks {TrustedNetworks}, API docs {ApiDocs}, admin API {AdminApi}",
                app.Urls,
                signing.KeyId,
                additionalKeyId,
                app.Services.GetRequiredService<IOptions<RegistryOptions>>().Value.Url,
                platform.MeteringUrl,
                clients.Authority,
                clients.Audience,
                clients.ClientIdClaim,
                clients.ScopeClaim,
                controlPlane.Url,
                controlPlane.ClientCacheLifetime,
                moduleTokens.Lifetime,
                usage.EffectiveRetryDelays,
                proxies.Proxies.Count == 0 ? None : string.Join(", ", proxies.Proxies),
                proxies.Networks.Count == 0 ? None : string.Join(", ", proxies.Networks),
                docs.Enabled ? "on" : "off",
                admin.Port is { } adminPort
                    ? $"on port {adminPort} for operator tokens from {admin.Authority} for {admin.Audience} with {admin.ClaimType} = {admin.ClaimValue}"
                    : "off");
        });
    }
}
