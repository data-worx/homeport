// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Homeport.Sdk.Tests.Integration;

/// <summary>
/// A module built with the SDK, in memory and in the <c>Production</c>
/// environment, so nothing passes only because it runs in Development. Its
/// endpoints are the controllers of this assembly.
/// </summary>
internal static class TestModule
{
    internal static readonly ModuleManifest Manifest = new(
        Id: "things",
        Name: "Things",
        Version: "1.2.3",
        Routes: [new ManifestRoute("POST", "/v1/things", "things.write", "requests", 2)],
        Scopes: ["things.write"],
        Meters: ["requests"]);

    internal static WebApplicationBuilder CreateBuilder(ModuleManifest manifest)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });
        builder.WebHost.UseTestServer();
        builder.AddHomeportModule(manifest);

        return builder;
    }

    internal static async Task<WebApplication> StartAsync(ILoggerProvider? logs = null)
    {
        var builder = CreateBuilder(Manifest);
        builder.Services.AddControllers().AddApplicationPart(typeof(TestModule).Assembly);
        if (logs is not null)
        {
            builder.Logging.AddProvider(logs);
        }

        var app = builder.Build();
        app.MapHomeportModule();
        app.MapControllers();
        await app.StartAsync();

        return app;
    }
}
