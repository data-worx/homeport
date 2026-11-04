// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Homeport.Sdk.Registration;
using Homeport.Shared.Authentication;
using Homeport.Shared.Client.ModuleContract;
using Homeport.Shared.Client.Problems;
using Homeport.Shared.ServiceDefaults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Homeport.Sdk;

/// <summary>
/// Turns a web application into a Homeport module: the service defaults, the
/// manifest, the health endpoints, the OpenAPI document, the Problem Details
/// error responses and the module token the module contract requires.
/// </summary>
public static class HomeportModuleExtensions
{
    /// <summary>Adds what a module needs, including its registration at the registry.</summary>
    /// <remarks>
    /// Every error response becomes Problem Details carrying the
    /// <see cref="ProblemConventions.TraceIdExtension"/>; an unhandled exception
    /// becomes <see cref="HomeportProblems.Internal"/>, and a controller request
    /// that fails validation becomes <see cref="HomeportProblems.Validation"/>.
    /// <para>
    /// Every operation of the OpenAPI document lists the errors every module
    /// endpoint can answer with. The module calls <c>AddOpenApi</c> itself:
    /// the XML docs of its controllers reach the document only through a call
    /// in its own project, where the framework's source generator intercepts it.
    /// </para>
    /// <para>
    /// Every request needs a valid module token: signed by the gateway, whose
    /// JWKS is read from <c>GatewayTokens:JwksUri</c>, for the manifest's id as
    /// audience. A request without one is answered with <c>401</c>, unless its
    /// endpoint allows anonymous callers. Every scope of the manifest is an
    /// authorization policy of the same name, so an endpoint requires one with
    /// <c>[Authorize(Policy = "&lt;scope&gt;")]</c>. <see cref="HomeportCaller.From"/>
    /// reads tenant, client, subject and scopes from the token.
    /// </para>
    /// <para>
    /// Once started, the instance registers at the registry named in
    /// <c>Homeport:Registry:Url</c> with <c>Homeport:Registry:Key</c> and
    /// <c>Homeport:Registry:InstanceAddress</c>, and sends a heartbeat every
    /// 10 s. The registry checks the manifest; when it refuses the
    /// registration, the module logs why and stops.
    /// </para>
    /// </remarks>
    public static TBuilder AddHomeportModule<TBuilder>(this TBuilder builder, ModuleManifest manifest)
        where TBuilder : IHostApplicationBuilder
    {
        builder.AddServiceDefaults();
        builder.Services.AddSingleton(manifest);
        AddRegistration(builder.Services);
        AddModuleToken(builder.Services, manifest);
        builder.Services.ConfigureAll<OpenApiOptions>(options => options.AddOperationTransformer<CommonErrorResponses>());
        AddProblemDetails(builder.Services);

        return builder;
    }

    /// <summary>
    /// Maps <see cref="ModuleManifest.ManifestPath"/>, <see cref="ModuleManifest.OpenApiPath"/>
    /// and the health endpoints.
    /// </summary>
    /// <remarks>
    /// All of them are anonymous on purpose: the registry and the gateway read the
    /// manifest and the OpenAPI document, and container runtimes probe health,
    /// before any token exists. They describe the module, never tenant data.
    /// <para>
    /// They are mapped here rather than served by controllers (<c>dotnet-core.web</c>):
    /// they are contract plumbing a module neither writes nor changes, like the
    /// framework-mapped endpoints the rule exempts, and a controller would have to
    /// be pushed into every module as an application part.
    /// </para>
    /// <para>
    /// It also puts the error handling and the module token in front of the
    /// module's endpoints, so it is called before they are mapped: an exception
    /// becomes <see cref="HomeportProblems.Internal"/>, and an error status
    /// without a body, such as a <c>404</c> for an unknown path or a <c>401</c>
    /// for a missing token, gets a Problem Details body.
    /// </para>
    /// <para>Once the module has started, it logs the manifest it serves.</para>
    /// </remarks>
    public static WebApplication MapHomeportModule(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapDefaultEndpoints();

        app.MapGet(ModuleManifest.ManifestPath, (ModuleManifest manifest) => TypedResults.Ok(manifest))
            .ExcludeFromDescription()
            .AllowAnonymous();

        app.MapOpenApi(ModuleManifest.OpenApiPath)
            .AllowAnonymous();

        LogStart(app);

        return app;
    }

    private static void LogStart(WebApplication app)
    {
        var manifest = app.Services.GetRequiredService<ModuleManifest>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(HomeportModuleExtensions));

        app.Lifetime.ApplicationStarted.Register(() => logger.LogInformation(
            "Module {ModuleId} {Version} started on {Urls}: contract version {ContractVersion}, {RouteCount} routes",
            manifest.Id,
            manifest.Version,
            app.Urls,
            manifest.ContractVersion,
            manifest.Routes.Count));
    }

    private static void AddRegistration(IServiceCollection services)
    {
        services.AddOptions<RegistrationOptions>()
            .BindConfiguration(RegistrationOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<RegistrationOptions>, RegistrationOptionsValidator>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpClient(ModuleRegistration.ClientName);
        services.AddHostedService<ModuleRegistration>();
    }

    private static void AddModuleToken(IServiceCollection services, ModuleManifest manifest)
    {
        services.AddGatewayTokenAuthentication(manifest.Id);

        // An endpoint the module forgets to mark is closed rather than open.
        var authorization = services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        foreach (var scope in manifest.Scopes)
        {
            authorization.AddPolicy(scope, policy => policy
                .RequireAuthenticatedUser()
                .RequireAssertion(context => HomeportCaller.From(context.User).HasScope(scope)));
        }
    }

    private static void AddProblemDetails(IServiceCollection services)
    {
        // The W3C trace id, rather than ASP.NET's default of the whole traceparent,
        // so a caller can look the trace up by it.
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Extensions[ProblemConventions.TraceIdExtension] =
                Activity.Current?.TraceId.ToHexString() ?? context.HttpContext.TraceIdentifier);
        services.AddExceptionHandler<InternalErrorHandler>();

        // Post-configured, because AddControllers, which the module calls after
        // this, configures it itself and would overwrite it.
        services.PostConfigure<ApiBehaviorOptions>(options =>
            options.InvalidModelStateResponseFactory = ValidationProblems.Create);
    }
}
