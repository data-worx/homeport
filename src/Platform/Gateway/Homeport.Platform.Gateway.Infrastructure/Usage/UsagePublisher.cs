// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.Extensions.Logging;
using Wolverine;

namespace Homeport.Platform.Gateway.Infrastructure.Usage;

/// <summary>
/// Hands a usage event to Wolverine, which buffers it in memory and sends it
/// to <see cref="UsageMessaging.Exchange"/> in the background. A request never
/// waits for the broker.
/// </summary>
public sealed class UsagePublisher(IMessageBus bus, UsageMetrics metrics, ILogger<UsagePublisher> logger)
{
    /// <summary>Publishes <paramref name="usage"/>; a failure is counted as dropped, never thrown.</summary>
    public async Task PublishAsync(UsageRecorded usage)
    {
        try
        {
            await bus.PublishAsync(usage);
        }
        catch (Exception exception)
        {
            metrics.Dropped(UsageMetrics.NotPublished);
            logger.LogWarning(exception, "Usage event {EventId} could not be published and is dropped", usage.EventId);
        }
    }
}
