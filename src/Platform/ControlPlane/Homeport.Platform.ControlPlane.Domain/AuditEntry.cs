// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain;

/// <summary>
/// What an operator changed, as it was at the time (ADR-0020). Homeport only
/// ever adds entries; it never changes or deletes one.
/// </summary>
public sealed class AuditEntry
{
    /// <summary>The longest subject, display name, action or target.</summary>
    public const int MaxFieldLength = 255;

    /// <summary>The longest summary.</summary>
    public const int MaxSummaryLength = 1000;

    /// <summary>A tenant was created.</summary>
    public const string TenantCreated = "tenant.created";

    /// <summary>A tenant was renamed.</summary>
    public const string TenantRenamed = "tenant.renamed";

    public required Guid Id { get; init; }

    /// <summary>When the change was made.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The operator's subject at the identity provider.</summary>
    public required string OperatorSubject { get; init; }

    /// <summary>The operator's display name at the time of the change.</summary>
    public required string OperatorName { get; init; }

    /// <summary>What was done, such as <see cref="TenantCreated"/>.</summary>
    public required string Action { get; init; }

    /// <summary>What it was done to, such as <c>tenants/{id}</c>.</summary>
    public required string Target { get; init; }

    /// <summary>The change in one English sentence, for a reader of the audit log.</summary>
    public required string Summary { get; init; }

    /// <summary>The <see cref="Target"/> that names the tenant <paramref name="tenantId"/>.</summary>
    public static string TenantTarget(Guid tenantId)
    {
        return $"tenants/{tenantId}";
    }
}
