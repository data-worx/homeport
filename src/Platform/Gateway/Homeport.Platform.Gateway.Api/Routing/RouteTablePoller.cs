// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.ApiDocs;
using Homeport.Platform.Gateway.Infrastructure.Registry;
using Yarp.ReverseProxy.Configuration;

namespace Homeport.Platform.Gateway.Api.Routing;

/// <summary>
/// Reads the route table from the registry every <see cref="PollInterval"/>
/// and rebuilds the proxy configuration when it changed (ADR-0004). While the
/// registry cannot be read, the gateway keeps serving the last known routes,
/// and the API docs stay as they are. The <see cref="PlatformRoutes"/> stand
/// next to the module routes in every configuration.
/// </summary>
internal sealed class RouteTablePoller(
    RegistryClient registry,
    InMemoryConfigProvider proxy,
    PlatformRoutes platform,
    ApiDocsRefresher docs,
    TimeProvider time,
    ILogger<RouteTablePoller> logger)
    : BackgroundService
{
    /// <summary>How often the registry is asked; a stopped instance disappears within its TTL plus this.</summary>
    internal static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private string? etag;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, time);
        do
        {
            await PollAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task PollAsync(CancellationToken stoppingToken)
    {
        try
        {
            var read = await registry.ReadRoutesAsync(etag, stoppingToken);
            if (read is null)
            {
                logger.LogDebug("The routes are unchanged since {ETag}", etag);
                return;
            }

            var (routes, clusters) = ModuleRoutes.Build(read.Table);
            var (platformRoutes, platformClusters) = platform.Build();
            proxy.Update([.. platformRoutes, .. routes], [.. platformClusters, .. clusters]);
            etag = read.ETag;
            docs.RoutesChanged(read.Table);

            logger.LogInformation(
                "Routes changed: {RouteCount} routes of modules {ModuleIds}",
                routes.Count,
                read.Table.Modules.Select(module => module.ModuleId));
        }
        catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
        {
            // Unreachable, timed out or answering with an error: the routes stay as they are.
            logger.LogWarning(
                exception,
                "The routes could not be read from the registry; keeping the last known {RouteCount} routes",
                proxy.GetConfig().Routes.Count);
        }
    }
}
