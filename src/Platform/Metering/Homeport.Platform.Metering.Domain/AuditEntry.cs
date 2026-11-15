// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Domain;

/// <summary>
/// What an operator read of the tenants' usage, as it was at the time
/// (ADR-0020). Homeport only ever adds entries; it never changes or deletes
/// one.
/// </summary>
public sealed class AuditEntry
{
    /// <summary>The longest subject, display name, action or target.</summary>
    public const int MaxFieldLength = 255;

    /// <summary>The longest summary.</summary>
    public const int MaxSummaryLength = 1000;

    /// <summary>The usage of every tenant, or of one, was read.</summary>
    public const string UsageRead = "usage.read";

    /// <summary>The <see cref="Target"/> of a read across every tenant.</summary>
    public const string AllTenantsTarget = "tenants";

    /// <summary>The entry's id.</summary>
    public required Guid Id { get; init; }

    /// <summary>When the read was made.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The operator's subject at the identity provider.</summary>
    public required string OperatorSubject { get; init; }

    /// <summary>The operator's display name at the time of the read.</summary>
    public required string OperatorName { get; init; }

    /// <summary>What was done, such as <see cref="UsageRead"/>.</summary>
    public required string Action { get; init; }

    /// <summary>Whose data it was done to: <see cref="AllTenantsTarget"/> or <c>tenants/{id}</c>.</summary>
    public required string Target { get; init; }

    /// <summary>The read in one English sentence, for a reader of the audit log.</summary>
    public required string Summary { get; init; }

    /// <summary>The <see cref="Target"/> that names the tenant <paramref name="tenantId"/>.</summary>
    public static string TenantTarget(Guid tenantId)
    {
        return $"{AllTenantsTarget}/{tenantId}";
    }
}
