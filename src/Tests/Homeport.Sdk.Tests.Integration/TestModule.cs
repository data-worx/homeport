// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using Homeport.Shared.Client.ModuleContract;
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
    internal const string WriteScope = "things.write";

    internal static readonly ModuleManifest Manifest = new(
        Id: "things",
        Name: "Things",
        Version: "1.2.3",
        Routes: [new ManifestRoute("POST", "/v1/things", WriteScope, "requests", 2)],
        Scopes: [WriteScope],
        Meters: ["requests"]);

    internal static WebApplicationBuilder CreateBuilder(ModuleManifest manifest)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
        });
        builder.WebHost.UseTestServer();
        builder.AddHomeportModule(manifest);
        builder.Services.AddOpenApi();

        return builder;
    }

    /// <summary>The module, accepting the module tokens of <paramref name="gateway"/>.</summary>
    internal static Task<WebApplication> StartAsync(StandInGateway gateway)
    {
        return StartAsync(configure: builder => builder.Configuration["GatewayTokens:JwksUri"] = gateway.JwksUri);
    }

    /// <summary>Sends a GET to <paramref name="path"/> of <paramref name="app"/> with <paramref name="token"/>.</summary>
    internal static Task<HttpResponseMessage> GetAsync(WebApplication app, string path, string token)
    {
        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return client.GetAsync(path);
    }

    internal static async Task<WebApplication> StartAsync(ILoggerProvider? logs = null, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = CreateBuilder(Manifest);
        configure?.Invoke(builder);
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
