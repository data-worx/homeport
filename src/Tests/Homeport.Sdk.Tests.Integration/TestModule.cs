// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Hosting;

namespace Homeport.Sdk.Tests.Integration;

/// <summary>
/// A module built with the SDK, in memory and in the <c>Production</c>
/// environment, so nothing passes only because it runs in Development.
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

    internal static async Task<WebApplication> StartAsync()
    {
        var app = CreateBuilder(Manifest).Build();
        app.MapHomeportModule();
        await app.StartAsync();

        return app;
    }
}
