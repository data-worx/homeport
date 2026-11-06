// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Threading.Channels;
using Homeport.Shared.Internal.Platform.Registry;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.ApiDocs;

/// <summary>
/// Rebuilds the API docs whenever the route table changed, apart from the
/// poller, so routing never waits for a module's document. Only the newest
/// table counts: one that arrives during a rebuild replaces any still waiting.
/// Does nothing while the docs are off.
/// </summary>
internal sealed class ApiDocsRefresher(
    ApiDocsCatalog catalog,
    IOptions<ApiDocsOptions> options,
    ILogger<ApiDocsRefresher> logger)
    : BackgroundService
{
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
        await foreach (var table in tables.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await catalog.RefreshAsync(table, stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                // The docs stay as they were; routing is not affected.
                logger.LogError(exception, "The API docs could not be rebuilt; keeping the last ones");
            }
        }
    }
}
