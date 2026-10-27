// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.ServiceDefaults;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Homeport.Sdk;

/// <summary>
/// Turns a web application into a Homeport module: the service defaults, the
/// manifest, the health endpoints and the OpenAPI document the module contract
/// requires.
/// </summary>
public static class HomeportModuleExtensions
{
    /// <summary>Adds what a module needs, after checking <paramref name="manifest"/> against the contract.</summary>
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
    /// </remarks>
    public static WebApplication MapHomeportModule(this WebApplication app)
    {
        app.MapDefaultEndpoints();

        app.MapGet(ModuleManifest.ManifestPath, (ModuleManifest manifest) => TypedResults.Ok(manifest))
            .ExcludeFromDescription()
            .AllowAnonymous();

        app.MapOpenApi(ModuleManifest.OpenApiPath)
            .AllowAnonymous();

        return app;
    }
}
