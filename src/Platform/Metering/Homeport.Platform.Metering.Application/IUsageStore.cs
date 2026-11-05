// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Application;

/// <summary>Where usage events and their aggregates are kept, for the event's tenant only.</summary>
public interface IUsageStore
{
    /// <summary>
    /// In one transaction: keeps <paramref name="usage"/> and adds
    /// <paramref name="hourly"/> and <paramref name="daily"/> to the
    /// aggregates, unless an event with the same id is kept already.
    /// </summary>
    /// <returns>Whether the event was new; a duplicate changes nothing.</returns>
    Task<bool> RecordAsync(UsageEvent usage, HourlyUsage hourly, DailyUsage daily, CancellationToken cancellationToken);
}
