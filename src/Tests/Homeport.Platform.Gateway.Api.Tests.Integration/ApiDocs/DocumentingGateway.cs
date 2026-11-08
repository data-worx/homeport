// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using System.Text.Json.Nodes;
using Homeport.Platform.Gateway.Api.ApiDocs;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Internal.Platform.Metering;
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
/// <see cref="UndeclaredPath"/>, which both documents describe. A stand-in
/// metering serves its document to a bearer token only, describing the usage
/// export and <see cref="UnroutedPlatformPath"/>, which the gateway does not
/// route. The poller runs on <see cref="Time"/>, the platform docs on
/// <see cref="PlatformTime"/>; a test advances them.
/// </summary>
public sealed class DocumentingGateway : IAsyncLifetime
{
    internal const string SharedPath = "/v1/shared";
    internal const string OldPath = "/v1/old";
    internal const string UndeclaredPath = "/v1/undeclared";
    internal const string UnroutedPlatformPath = "/v1/unrouted";
    internal const string OldVersion = "1.0.0";
    internal const string NewVersion = "2.0.0";
    internal const string OpenApiPath = "/openapi/v1.json";

    private const string LocalAddress = "http://127.0.0.1:0";
    private const string BearerPrefix = "Bearer ";
    private const string CatalogCategory = "Homeport.Platform.Gateway.Api.ApiDocs.ApiDocsCatalog";
    private const string ModulesRebuilt = "API docs rebuilt for modules";
    private const string PlatformRebuilt = "Platform API docs rebuilt";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WaitPoll = TimeSpan.FromMilliseconds(20);

    private WebApplication? oldInstance;
    private WebApplication? newInstance;
    private WebApplication? metering;
    private StandInRegistry? registry;
    private WebApplicationFactory<Program>? gateway;

    internal FakeTimeProvider Time { get; } = new(TimeProvider.System.GetUtcNow());

    internal FakeTimeProvider PlatformTime { get; } = new(TimeProvider.System.GetUtcNow());

    internal StandInRegistry Registry => registry ?? throw new InvalidOperationException("The registry has not been started.");

    internal WebApplicationFactory<Program> Gateway => gateway ?? throw new InvalidOperationException("The gateway has not been started.");

    /// <summary>
    /// When set, the older instance answers its document with <c>404</c>, a
    /// failure no resilience handler retries.
    /// </summary>
    internal bool OldDocumentFails { get; set; }

    /// <summary>When set, metering answers its document with <c>404</c>.</summary>
    internal bool MeteringDocumentFails { get; set; }

    /// <summary>The bearer token metering last answered its document to; <c>null</c> before.</summary>
    internal string? MeteringToken { get; private set; }

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
        metering = await StartMeteringAsync();
        registry = await StandInRegistry.StartAsync(Table());

        gateway = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host
                .WithUsage().WithTokenBuckets()
                .WithSigningKey()
                .WithRegistry(registry)
                .WithIdentityProvider(GatewaySettings.UnusedAuthority)
                .UseSetting($"{PlatformRoutesOptions.SectionName}:{nameof(PlatformRoutesOptions.MeteringUrl)}", metering.Urls.Single())
                .ConfigureLogging(logging => logging.AddFakeLogging())
                .ConfigureTestServices(UseTestClocks));
        await WaitForDocsAsync(1);
        await WaitForPlatformDocsAsync(1);
    }

    /// <summary>Waits until the gateway has rebuilt its module documents <paramref name="count"/> times.</summary>
    internal Task WaitForDocsAsync(int count)
    {
        return WaitForCatalogAsync(LogLevel.Information, ModulesRebuilt, count);
    }

    /// <summary>Waits until the gateway has rebuilt a changed platform document <paramref name="count"/> times.</summary>
    internal Task WaitForPlatformDocsAsync(int count)
    {
        return WaitForCatalogAsync(LogLevel.Information, PlatformRebuilt, count);
    }

    /// <summary>Waits until the gateway has failed to read a document <paramref name="count"/> times.</summary>
    internal Task WaitForUnreadDocumentsAsync(int count)
    {
        return WaitForCatalogAsync(LogLevel.Warning, string.Empty, count);
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

        foreach (var instance in new[] { oldInstance, newInstance, metering })
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
            : Results.Content(document, MediaTypeNames.Application.Json));
        await instance.StartAsync();

        return instance;
    }

    // Metering's document describes the usage export with a query parameter
    // and both media types, and an operation the gateway does not route.
    private async Task<WebApplication> StartMeteringAsync()
    {
        var document = new JsonObject
        {
            ["openapi"] = "3.1.1",
            ["info"] = new JsonObject { ["title"] = "Metering", ["version"] = "1.0.0" },
            ["paths"] = new JsonObject
            {
                [UsageApi.Path] = new JsonObject
                {
                    ["get"] = new JsonObject
                    {
                        ["operationId"] = "Usage",
                        ["parameters"] = new JsonArray(new JsonObject { ["name"] = UsageApi.FromParameter, ["in"] = "query" }),
                        ["responses"] = new JsonObject
                        {
                            ["200"] = new JsonObject
                            {
                                ["content"] = new JsonObject
                                {
                                    [MediaTypeNames.Application.Json] = new JsonObject(),
                                    [UsageApi.CsvMediaType] = new JsonObject(),
                                },
                            },
                        },
                    },
                },
                [UnroutedPlatformPath] = new JsonObject { ["get"] = new JsonObject { ["operationId"] = "Unrouted" } },
            },
        }.ToJsonString();

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        var instance = builder.Build();
        instance.MapGet(MeteringService.OpenApiPath, (HttpRequest request) =>
        {
            var authorization = request.Headers.Authorization.ToString();
            if (!authorization.StartsWith(BearerPrefix, StringComparison.Ordinal))
            {
                return Results.StatusCode(StatusCodes.Status401Unauthorized);
            }

            MeteringToken = authorization[BearerPrefix.Length..];
            return MeteringDocumentFails
                ? Results.StatusCode(StatusCodes.Status404NotFound)
                : Results.Content(document, MediaTypeNames.Application.Json);
        });
        await instance.StartAsync();

        return instance;
    }

    private async Task WaitForCatalogAsync(LogLevel level, string messagePrefix, int count)
    {
        var logs = Gateway.Services.GetFakeLogCollector();
        using var timeout = new CancellationTokenSource(WaitTimeout);
        while (logs.GetSnapshot().Count(entry => entry.Category == CatalogCategory
            && entry.Level == level
            && entry.Message.StartsWith(messagePrefix, StringComparison.Ordinal)) < count)
        {
            await Task.Delay(WaitPoll, timeout.Token);
        }
    }

    // Each fake clock only to the component it drives (docs/architecture/testing.md).
    private void UseTestClocks(IServiceCollection services)
    {
        var poller = services.Single(service => service.ImplementationType == typeof(RouteTablePoller));
        services[services.IndexOf(poller)] = ServiceDescriptor.Singleton<IHostedService>(
            provider => ActivatorUtilities.CreateInstance<RouteTablePoller>(provider, Time));

        var refresher = services.Single(service => service.ServiceType == typeof(ApiDocsRefresher));
        services[services.IndexOf(refresher)] = ServiceDescriptor.Singleton(
            provider => ActivatorUtilities.CreateInstance<ApiDocsRefresher>(provider, PlatformTime));
    }
}
