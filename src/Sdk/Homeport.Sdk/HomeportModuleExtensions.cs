// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Homeport.Shared.Contracts;
using Homeport.Shared.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Homeport.Sdk;

/// <summary>
/// Turns a web application into a Homeport module: the service defaults, the
/// manifest, the health endpoints, the OpenAPI document and the Problem Details
/// error responses the module contract requires.
/// </summary>
public static class HomeportModuleExtensions
{
    /// <summary>Adds what a module needs, after checking <paramref name="manifest"/> against the contract.</summary>
    /// <remarks>
    /// Every error response becomes Problem Details carrying the
    /// <see cref="ProblemConventions.TraceIdExtension"/>; an unhandled exception
    /// becomes <see cref="HomeportProblems.Internal"/>, and a controller request
    /// that fails validation becomes <see cref="HomeportProblems.Validation"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The manifest breaks the module contract.</exception>
    public static TBuilder AddHomeportModule<TBuilder>(this TBuilder builder, ModuleManifest manifest)
        where TBuilder : IHostApplicationBuilder
    {
        // A broken manifest is a programming error, so the module refuses to start
        // rather than announce something it does not serve.
        var problems = manifest.Validate();
        if (problems.Count > 0)
        {
            throw new InvalidOperationException(
                $"The manifest of module '{manifest.Id}' breaks the module contract:{Environment.NewLine}"
                + string.Join(Environment.NewLine, problems));
        }

        builder.AddServiceDefaults();
        builder.Services.AddSingleton(manifest);
        builder.Services.AddOpenApi();
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
    /// It also puts the error handling in front of the module's endpoints, so it
    /// is called before they are mapped: an exception becomes
    /// <see cref="HomeportProblems.Internal"/>, and an error status without a
    /// body, such as a <c>404</c> for an unknown path, gets a Problem Details body.
    /// </para>
    /// </remarks>
    public static WebApplication MapHomeportModule(this WebApplication app)
    {
        app.UseExceptionHandler();
        app.UseStatusCodePages();

        app.MapDefaultEndpoints();

        app.MapGet(ModuleManifest.ManifestPath, (ModuleManifest manifest) => TypedResults.Ok(manifest))
            .ExcludeFromDescription()
            .AllowAnonymous();

        app.MapOpenApi(ModuleManifest.OpenApiPath)
            .AllowAnonymous();

        return app;
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
