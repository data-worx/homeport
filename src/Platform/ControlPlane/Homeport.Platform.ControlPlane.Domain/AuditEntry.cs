// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Domain;

/// <summary>
/// What an operator changed, as it was at the time (ADR-0020). Homeport only
/// ever adds entries; it never changes or deletes one.
/// </summary>
public sealed class AuditEntry
{
    /// <summary>The longest subject, display name or action.</summary>
    public const int MaxFieldLength = 255;

    /// <summary>The longest target: the longest client id behind its prefix.</summary>
    public const int MaxTargetLength = 263;

    /// <summary>The longest summary.</summary>
    public const int MaxSummaryLength = 1000;

    /// <summary>A tenant was created.</summary>
    public const string TenantCreated = "tenant.created";

    /// <summary>A tenant was renamed.</summary>
    public const string TenantRenamed = "tenant.renamed";

    /// <summary>A plan was created.</summary>
    public const string PlanCreated = "plan.created";

    /// <summary>A plan's name or limits were changed.</summary>
    public const string PlanChanged = "plan.changed";

    /// <summary>A plan was deleted.</summary>
    public const string PlanDeleted = "plan.deleted";

    /// <summary>A client was registered for a tenant and a plan.</summary>
    public const string ClientRegistered = "client.registered";

    /// <summary>A client was moved to another plan.</summary>
    public const string ClientPlanChanged = "client.plan-changed";

    /// <summary>A client was removed.</summary>
    public const string ClientRemoved = "client.removed";

    // Its length and Client.MaxClientIdLength add up to MaxTargetLength.
    private const string ClientTargetPrefix = "clients/";

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

    /// <summary>The <see cref="Target"/> that names the plan <paramref name="planId"/>.</summary>
    public static string PlanTarget(Guid planId)
    {
        return $"plans/{planId}";
    }

    /// <summary>The <see cref="Target"/> that names the client <paramref name="clientId"/>.</summary>
    public static string ClientTarget(string clientId)
    {
        return ClientTargetPrefix + clientId;
    }
}
