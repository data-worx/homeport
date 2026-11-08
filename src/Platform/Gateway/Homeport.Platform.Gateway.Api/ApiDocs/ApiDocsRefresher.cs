// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Channels;
using Homeport.Shared.Internal.Platform.Registry;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.ApiDocs;

/// <summary>
/// Rebuilds the module documents whenever the route table changed, apart
/// from the poller, so routing never waits for a module's document. Only the
/// newest table counts: one that arrives during a rebuild replaces any still
/// waiting. Rebuilds the platform's document at start and every
/// <see cref="PlatformInterval"/>, as the platform routes never change but
/// the services behind them start late or are updated. Does nothing while the
/// docs are off.
/// </summary>
internal sealed class ApiDocsRefresher(
    ApiDocsCatalog catalog,
    IOptions<ApiDocsOptions> options,
    TimeProvider time,
    ILogger<ApiDocsRefresher> logger)
    : BackgroundService
{
    /// <summary>How often the documents of the platform services are read.</summary>
    internal static readonly TimeSpan PlatformInterval = TimeSpan.FromSeconds(30);

    private readonly Channel<RouteTableResponse> tables = Channel.CreateBounded<RouteTableResponse>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });

    /// <summary>Hands over the route table the gateway now routes on.</summary>
    internal void RoutesChanged(RouteTableResponse table)
    {
        if (options.Value.Enabled)
        {
            tables.Writer.TryWrite(table);
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        // Apart from each other: they rebuild different documents from different sources.
        await Task.WhenAll(RefreshModulesAsync(stoppingToken), RefreshPlatformAsync(stoppingToken));
    }

    private async Task RefreshModulesAsync(CancellationToken stoppingToken)
    {
        await foreach (var table in tables.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await catalog.RefreshAsync(table, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                // The docs stay as they were; routing is not affected.
                logger.LogError(exception, "The module API docs could not be rebuilt; keeping the last ones");
            }
        }
    }

    private async Task RefreshPlatformAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PlatformInterval, time);
        do
        {
            try
            {
                await catalog.RefreshPlatformAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "The platform API docs could not be rebuilt; keeping the last ones");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
