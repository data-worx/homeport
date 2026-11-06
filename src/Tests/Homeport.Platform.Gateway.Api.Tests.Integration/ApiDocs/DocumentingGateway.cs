// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Nodes;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Shared.Internal.Platform.Registry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.ApiDocs;

/// <summary>
/// The gateway in memory with the API docs switched on, in front of two
/// stand-in Echo instances of different versions that serve an OpenAPI
/// document each. Both declare <see cref="SharedPath"/>, which the newer one
/// documents; only the older declares <see cref="OldPath"/>; neither declares
/// <see cref="UndeclaredPath"/>, which both documents describe. The poller
/// runs on <see cref="Time"/>, which a test advances.
/// </summary>
public sealed class DocumentingGateway : IAsyncLifetime
{
    internal const string SharedPath = "/v1/shared";
    internal const string OldPath = "/v1/old";
    internal const string UndeclaredPath = "/v1/undeclared";
    internal const string OldVersion = "1.0.0";
    internal const string NewVersion = "2.0.0";
    internal const string OpenApiPath = "/openapi/v1.json";

    private const string LocalAddress = "http://127.0.0.1:0";
    private const string CatalogCategory = "Homeport.Platform.Gateway.Api.ApiDocs.ApiDocsCatalog";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WaitPoll = TimeSpan.FromMilliseconds(20);

    private WebApplication? oldInstance;
    private WebApplication? newInstance;
    private StandInRegistry? registry;
    private WebApplicationFactory<Program>? gateway;

    internal FakeTimeProvider Time { get; } = new(TimeProvider.System.GetUtcNow());

    internal StandInRegistry Registry => registry ?? throw new InvalidOperationException("The registry has not been started.");

    internal WebApplicationFactory<Program> Gateway => gateway ?? throw new InvalidOperationException("The gateway has not been started.");

    /// <summary>
    /// When set, the older instance answers its document with <c>404</c>, a
    /// failure no resilience handler retries.
    /// </summary>
    internal bool OldDocumentFails { get; set; }

    /// <summary>The route table of both instances, plus <paramref name="extraPaths"/> on the older one.</summary>
    internal RouteTableResponse Table(params string[] extraPaths)
    {
        var oldAddress = new Uri(oldInstance!.Urls.Single());
        var newAddress = new Uri(newInstance!.Urls.Single());
        var oldUrl = new Uri(oldAddress, OpenApiPath);
        var newUrl = new Uri(newAddress, OpenApiPath);

        IEnumerable<RouteResponse> routes =
        [
            new(HttpMethods.Get, OldPath, "echo.invoke", "requests", 1, [oldAddress], OldVersion, oldUrl),
            new(HttpMethods.Get, SharedPath, "echo.invoke", "requests", 1, [oldAddress, newAddress], NewVersion, newUrl),
            .. extraPaths.Select(path => new RouteResponse(HttpMethods.Get, path, "echo.invoke", "requests", 1, [oldAddress], OldVersion, oldUrl)),
        ];

        return new RouteTableResponse([new ModuleRoutesResponse("echo", routes.ToList())]);
    }

    public async Task InitializeAsync()
    {
        oldInstance = await StartInstanceAsync(OldVersion, () => OldDocumentFails);
        newInstance = await StartInstanceAsync(NewVersion, () => false);
        registry = await StandInRegistry.StartAsync(Table());

        gateway = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host
                .WithUsage()
                .WithSigningKey()
                .WithRegistry(registry)
                .WithIdentityProvider(GatewaySettings.UnusedAuthority)
                .ConfigureLogging(logging => logging.AddFakeLogging())
                .ConfigureTestServices(UseTestClockForPoller));
        await WaitForDocsAsync(1);
    }

    /// <summary>Waits until the gateway has rebuilt its docs <paramref name="count"/> times.</summary>
    internal async Task WaitForDocsAsync(int count)
    {
        var logs = Gateway.Services.GetFakeLogCollector();
        using var timeout = new CancellationTokenSource(WaitTimeout);
        while (logs.GetSnapshot().Count(entry => entry.Category == CatalogCategory && entry.Level == LogLevel.Information) < count)
        {
            await Task.Delay(WaitPoll, timeout.Token);
        }
    }

    /// <summary>The document called <paramref name="name"/>, as the gateway serves it.</summary>
    internal async Task<JsonObject> DocumentAsync(string name)
    {
        var json = await Gateway.CreateClient().GetStringAsync($"/platform/docs/openapi/{name}.json");

        return JsonNode.Parse(json)!.AsObject();
    }

    public async Task DisposeAsync()
    {
        if (gateway is not null)
        {
            await gateway.DisposeAsync();
        }

        if (registry is not null)
        {
            await registry.DisposeAsync();
        }

        foreach (var instance in new[] { oldInstance, newInstance })
        {
            if (instance is not null)
            {
                await instance.StopAsync();
                await instance.DisposeAsync();
            }
        }
    }

    // Each document describes the shared route with an operation id naming its
    // version, the undeclared route, and the old route.
    private static async Task<WebApplication> StartInstanceAsync(string version, Func<bool> fails)
    {
        var document = new JsonObject
        {
            ["openapi"] = "3.1.1",
            ["info"] = new JsonObject { ["title"] = "Echo", ["version"] = version },
            ["paths"] = new JsonObject
            {
                [SharedPath] = new JsonObject { ["get"] = new JsonObject { ["operationId"] = "Shared" + version } },
                [OldPath] = new JsonObject { ["get"] = new JsonObject { ["operationId"] = "Old" + version } },
                [UndeclaredPath] = new JsonObject { ["get"] = new JsonObject { ["operationId"] = "Undeclared" } },
            },
        }.ToJsonString();

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        var instance = builder.Build();
        instance.MapGet(OpenApiPath, () => fails()
            ? Results.StatusCode(StatusCodes.Status404NotFound)
            : Results.Content(document, "application/json"));
        await instance.StartAsync();

        return instance;
    }

    private void UseTestClockForPoller(IServiceCollection services)
    {
        var poller = services.Single(service => service.ImplementationType == typeof(RouteTablePoller));
        services[services.IndexOf(poller)] = ServiceDescriptor.Singleton<IHostedService>(
            provider => ActivatorUtilities.CreateInstance<RouteTablePoller>(provider, Time));
    }
}
