// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Homeport.Platform.Gateway.Infrastructure.Throttling;

/// <summary>
/// Connects the gateway to Valkey while it starts, before it serves a request.
/// The container connects when it first hands the connection out, and blocks
/// until that succeeded or timed out. Left to the first request, every request
/// arriving meanwhile holds a thread waiting for it; on a machine with few
/// cores they starve the thread pool the connect itself needs, it times out,
/// and the whole burst passes unthrottled although Valkey is up. Taking the
/// connection here has the host make it before any request is accepted.
/// While Valkey is down, the gateway starts once the connect timed out and
/// fails open until Valkey answers.
/// </summary>
internal sealed class ValkeyConnection(IConnectionMultiplexer valkey, ILogger<ValkeyConnection> logger) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (valkey.IsConnected)
        {
            logger.LogInformation("Connected to Valkey; requests are held to their tenant's rate limit");
        }
        else
        {
            logger.LogWarning("Valkey could not be reached at start; requests pass without a rate limit until it answers");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
