// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace Homeport.ServiceDefaults;

/// <summary>
/// What every Homeport service sets up the same way: OpenTelemetry over OTLP,
/// health checks, service discovery and resilient HTTP clients.
/// </summary>
public static class ServiceDefaultsExtensions
{
    /// <summary>Answers whether the process is alive. Runs only the checks tagged <c>live</c>.</summary>
    public const string LivenessPath = "/health/live";

    /// <summary>Answers whether the service can take traffic. Runs every check.</summary>
    public const string ReadinessPath = "/health/ready";

    private const string LiveTag = "live";

    /// <summary>Adds the service defaults to a host.</summary>
    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ConfigureOpenTelemetry(builder);

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [LiveTag]);

        builder.Services.AddServiceDiscovery();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        return builder;
    }

    /// <summary>
    /// Maps <see cref="LivenessPath"/> and <see cref="ReadinessPath"/>, which the
    /// module contract requires, in every environment.
    /// </summary>
    /// <remarks>
    /// Anonymous on purpose: container runtimes and orchestrators probe them
    /// without credentials. The response carries the overall status only, never
    /// the result of an individual check.
    /// </remarks>
    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(LivenessPath, new HealthCheckOptions { Predicate = check => check.Tags.Contains(LiveTag) })
            .AllowAnonymous();
        app.MapHealthChecks(ReadinessPath)
            .AllowAnonymous();

        return app;
    }

    private static void ConfigureOpenTelemetry(IHostApplicationBuilder builder)
    {
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        var telemetry = builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation())
            .WithTracing(tracing => tracing
                .AddSource(builder.Environment.ApplicationName)
                .AddAspNetCoreInstrumentation(aspNetCore =>
                    aspNetCore.Filter = context => !context.Request.Path.StartsWithSegments("/health"))
                .AddHttpClientInstrumentation());

        // The AppHost and the compose file set the endpoint; without one there is
        // nowhere to export to.
        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            telemetry.UseOtlpExporter();
        }
    }
}
