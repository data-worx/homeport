// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;

namespace Homeport.Platform.Metering.Domain.Usage;

/// <summary>
/// What a tenant asks to export: every bucket of <see cref="Granularity"/>
/// that starts in [<see cref="From"/>, <see cref="To"/>), optionally of one
/// module and one meter. Both bounds are in UTC.
/// </summary>
public sealed record UsageQuery
{
    /// <summary>The granularity is neither <c>hour</c> nor <c>day</c>.</summary>
    public const string UnknownGranularityCode = "usage.granularity";

    /// <summary>The period ends before it starts, or is empty.</summary>
    public const string EmptyPeriodCode = "usage.period";

    /// <summary>The name of <see cref="UsageGranularity.Hour"/> in a query.</summary>
    public const string HourName = "hour";

    /// <summary>The name of <see cref="UsageGranularity.Day"/> in a query.</summary>
    public const string DayName = "day";

    private UsageQuery(DateTimeOffset from, DateTimeOffset to, UsageGranularity granularity, string? moduleId, string? meter)
    {
        From = from;
        To = to;
        Granularity = granularity;
        ModuleId = moduleId;
        Meter = meter;
    }

    /// <summary>The first moment of the period, in UTC.</summary>
    public DateTimeOffset From { get; }

    /// <summary>The first moment after the period, in UTC.</summary>
    public DateTimeOffset To { get; }

    /// <summary>Which aggregates are read.</summary>
    public UsageGranularity Granularity { get; }

    /// <summary>Only this module's usage; every module's when <see langword="null"/>.</summary>
    public string? ModuleId { get; }

    /// <summary>Only this meter's usage; every meter's when <see langword="null"/>.</summary>
    public string? Meter { get; }

    /// <summary>The first day that starts in the period.</summary>
    public DateOnly FirstDay => DayStartingAtOrAfter(From);

    /// <summary>The first day that starts after the period.</summary>
    public DateOnly EndDay => DayStartingAtOrAfter(To);

    /// <summary>A query for the period [<paramref name="from"/>, <paramref name="to"/>).</summary>
    /// <param name="from">The first moment, in any offset.</param>
    /// <param name="to">The first moment after the period, in any offset.</param>
    /// <param name="granularity"><see cref="HourName"/> or <see cref="DayName"/>.</param>
    /// <param name="moduleId">Only this module; empty or <see langword="null"/> for every module.</param>
    /// <param name="meter">Only this meter; empty or <see langword="null"/> for every meter.</param>
    public static Result<UsageQuery> Create(DateTimeOffset from, DateTimeOffset to, string granularity, string? moduleId, string? meter)
    {
        UsageGranularity? parsed = granularity switch
        {
            HourName => UsageGranularity.Hour,
            DayName => UsageGranularity.Day,
            _ => null,
        };

        if (parsed is null)
        {
            return Result<UsageQuery>.Failure(new Error(UnknownGranularityCode, $"The granularity is '{HourName}' or '{DayName}'."));
        }

        if (to <= from)
        {
            return Result<UsageQuery>.Failure(new Error(EmptyPeriodCode, "The period ends after it starts."));
        }

        return Result<UsageQuery>.Success(new UsageQuery(
            from.ToUniversalTime(),
            to.ToUniversalTime(),
            parsed.Value,
            string.IsNullOrEmpty(moduleId) ? null : moduleId,
            string.IsNullOrEmpty(meter) ? null : meter));
    }

    // A day starts at UTC midnight: the day of the moment if it is midnight, else the next.
    private static DateOnly DayStartingAtOrAfter(DateTimeOffset moment)
    {
        var day = UsageBuckets.DayOf(moment);
        return moment.UtcDateTime.TimeOfDay == TimeSpan.Zero ? day : day.AddDays(1);
    }
}
