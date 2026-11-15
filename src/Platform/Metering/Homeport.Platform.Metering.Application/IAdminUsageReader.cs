// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain;
using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Application;

/// <summary>
/// Reads the aggregates of every tenant for the operator, past tenant
/// isolation (ADR-0020). The one way metering reads across tenants.
/// </summary>
public interface IAdminUsageReader
{
    /// <summary>
    /// The aggregates of <paramref name="tenantId"/>, or of every tenant when
    /// it is <see langword="null"/>, that <paramref name="query"/> selects,
    /// ordered by period, tenant, module and meter.
    /// </summary>
    /// <remarks>
    /// Adds <paramref name="audit"/> in the same transaction as the read:
    /// without the entry nothing is read, and a failed read leaves no entry.
    /// </remarks>
    Task<IReadOnlyList<TenantUsageTotal>> ReadAsync(Guid? tenantId, UsageQuery query, AuditEntry audit, CancellationToken cancellationToken);
}
