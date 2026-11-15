// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Homeport.Platform.Metering.Domain;
using Homeport.Platform.Metering.Domain.Usage;

namespace Homeport.Platform.Metering.Application;

/// <summary>
/// Metering's use case for the operator: reads the usage of every tenant, or
/// of one, and records the read in the audit log (ADR-0020).
/// </summary>
public sealed class UsageAdministration(IAdminUsageReader reader, TimeProvider clock)
{
    // ISO 8601 in UTC with Z, as the export writes a period.
    private const string MomentFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    /// <summary>
    /// The usage of <paramref name="tenantId"/>, or of every tenant when it is
    /// <see langword="null"/>, that <paramref name="query"/> selects, read by
    /// <paramref name="by"/>.
    /// </summary>
    public Task<IReadOnlyList<TenantUsageTotal>> ReadAsync(Guid? tenantId, UsageQuery query, Operator by, CancellationToken cancellationToken)
    {
        var target = tenantId is { } id ? AuditEntry.TenantTarget(id) : AuditEntry.AllTenantsTarget;
        var audit = by.Audit(clock.GetUtcNow(), AuditEntry.UsageRead, target, SummaryOf(tenantId, query));

        return reader.ReadAsync(tenantId, query, audit, cancellationToken);
    }

    /// <summary>The read in one English sentence, naming every filter it had.</summary>
    private static string SummaryOf(Guid? tenantId, UsageQuery query)
    {
        var granularity = query.Granularity == UsageGranularity.Hour ? "hourly" : "daily";
        var whose = tenantId is { } id ? $"the tenant {id}" : "every tenant";
        var module = query.ModuleId is { } moduleId ? $" of the module '{moduleId}'" : string.Empty;
        var meter = query.Meter is { } name ? $" in the meter '{name}'" : string.Empty;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"Read the {granularity} usage of {whose}{module}{meter} from {query.From.UtcDateTime.ToString(MomentFormat, CultureInfo.InvariantCulture)} to {query.To.UtcDateTime.ToString(MomentFormat, CultureInfo.InvariantCulture)}.");
    }
}
