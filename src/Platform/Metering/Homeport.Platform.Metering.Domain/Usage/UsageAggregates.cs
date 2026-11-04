// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Domain.Usage;

/// <summary>What one event adds to the aggregates: its quantity, in its hour and on its day.</summary>
public static class UsageAggregates
{
    /// <summary>The increment <paramref name="usage"/> adds to its tenant's, module's and meter's hour.</summary>
    public static HourlyUsage HourlyOf(UsageEvent usage)
    {
        return new HourlyUsage(usage.TenantId, usage.ModuleId, usage.Meter, UsageBuckets.HourOf(usage.OccurredAt), usage.Quantity);
    }

    /// <summary>The increment <paramref name="usage"/> adds to its tenant's, module's and meter's day.</summary>
    public static DailyUsage DailyOf(UsageEvent usage)
    {
        return new DailyUsage(usage.TenantId, usage.ModuleId, usage.Meter, UsageBuckets.DayOf(usage.OccurredAt), usage.Quantity);
    }
}
