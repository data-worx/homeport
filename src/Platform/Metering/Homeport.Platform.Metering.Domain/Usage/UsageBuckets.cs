// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Domain.Usage;

/// <summary>
/// The buckets usage is aggregated in: the hour and the day, both in UTC,
/// whatever offset the moment was recorded with.
/// </summary>
public static class UsageBuckets
{
    /// <summary>The start of the UTC hour <paramref name="occurredAt"/> falls in.</summary>
    public static DateTimeOffset HourOf(DateTimeOffset occurredAt)
    {
        var utc = occurredAt.UtcDateTime;
        return new DateTimeOffset(utc.Year, utc.Month, utc.Day, utc.Hour, 0, 0, TimeSpan.Zero);
    }

    /// <summary>The UTC day <paramref name="occurredAt"/> falls in.</summary>
    public static DateOnly DayOf(DateTimeOffset occurredAt)
    {
        return DateOnly.FromDateTime(occurredAt.UtcDateTime);
    }
}
