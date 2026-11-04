// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Application;

/// <summary>Metering's use case: counts each usage event once, in its hour and on its day (ADR-0007).</summary>
public sealed class UsageRecording(IUsageStore store)
{
    /// <summary>Records <paramref name="usage"/> and adds it to its aggregates, once.</summary>
    /// <returns>Whether the event was counted; <see langword="false"/> for a duplicate.</returns>
    public Task<bool> RecordAsync(UsageEvent usage, CancellationToken cancellationToken)
    {
        return store.RecordAsync(usage, UsageAggregates.HourlyOf(usage), UsageAggregates.DailyOf(usage), cancellationToken);
    }
}
