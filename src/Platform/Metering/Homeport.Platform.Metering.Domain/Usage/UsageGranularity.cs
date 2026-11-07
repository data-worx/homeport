// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Domain.Usage;

/// <summary>The buckets an export reads: the hourly or the daily aggregates.</summary>
public enum UsageGranularity
{
    /// <summary>One row per UTC hour.</summary>
    Hour,

    /// <summary>One row per UTC day.</summary>
    Day,
}
