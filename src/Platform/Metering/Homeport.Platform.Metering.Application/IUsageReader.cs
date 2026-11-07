// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Application;

/// <summary>Reads the aggregates of one tenant, and of no other.</summary>
public interface IUsageReader
{
    /// <summary>
    /// The aggregates of <paramref name="tenantId"/> that <paramref name="query"/>
    /// selects, ordered by period, module and meter.
    /// </summary>
    Task<IReadOnlyList<UsageTotal>> ReadAsync(Guid tenantId, UsageQuery query, CancellationToken cancellationToken);
}
