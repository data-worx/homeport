// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Domain.Usage;
using Homeport.Shared.Internal.Platform.Metering;
using Homeport.Shared.ServiceDefaults;

namespace Homeport.Platform.Metering.Api.Messaging;

/// <summary>
/// Consumes <see cref="UsageRecorded"/> from <see cref="UsageMessaging.MeteringQueue"/>
/// and counts it once, on a span that carries the event's tenant, client,
/// module and meter. Public, because Wolverine finds handlers by convention.
/// </summary>
public sealed class UsageRecordedHandler(UsageRecording recording, MeteringMetrics metrics, ILogger<UsageRecordedHandler> logger)
{
    /// <summary>Records the event; a duplicate is discarded.</summary>
    public async Task Handle(UsageRecorded message, CancellationToken cancellationToken)
    {
        Activity.Current?
            .SetTag(TelemetryAttributes.TenantId, message.TenantId.ToString())
            .SetTag(TelemetryAttributes.ClientId, message.ClientId)
            .SetTag(TelemetryAttributes.ModuleId, message.ModuleId)
            .SetTag(TelemetryAttributes.Meter, message.Meter);

        var recorded = await recording.RecordAsync(ToUsage(message), cancellationToken);
        metrics.Consumed(recorded);

        if (!recorded)
        {
            logger.LogDebug("Usage event {EventId} was counted before and is discarded", message.EventId);
        }
    }

    private static UsageEvent ToUsage(UsageRecorded message)
    {
        return new UsageEvent(
            message.EventId,
            message.OccurredAt,
            message.TenantId,
            message.ClientId,
            message.ModuleId,
            message.Route,
            message.Meter,
            message.Quantity,
            message.StatusCode,
            message.DurationMs,
            message.TraceId);
    }
}
