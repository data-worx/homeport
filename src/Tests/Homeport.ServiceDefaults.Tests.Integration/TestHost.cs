// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Homeport.ServiceDefaults.Tests.Integration;

/// <summary>
/// A web application with the service defaults, in memory and in the
/// <c>Production</c> environment, so nothing passes only because it runs in
/// Development.
/// </summary>
internal static class TestHost
{
    internal static async Task<WebApplication> StartAsync(HealthStatus readiness = HealthStatus.Healthy)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });
        builder.WebHost.UseTestServer();
        builder.AddServiceDefaults();
        builder.Services.AddHealthChecks()
            .AddCheck("dependency", () => new HealthCheckResult(readiness, "dependency detail"));

        var app = builder.Build();
        app.MapDefaultEndpoints();
        await app.StartAsync();

        return app;
    }
}
