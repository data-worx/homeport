// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Application;

/// <summary>Metering's use case for showback and chargeback: a tenant reads its own usage.</summary>
public sealed class UsageExport(IUsageReader reader)
{
    /// <summary>The usage of <paramref name="tenantId"/> that <paramref name="query"/> selects.</summary>
    public Task<IReadOnlyList<UsageTotal>> ReadAsync(Guid tenantId, UsageQuery query, CancellationToken cancellationToken)
    {
        return reader.ReadAsync(tenantId, query, cancellationToken);
    }
}
