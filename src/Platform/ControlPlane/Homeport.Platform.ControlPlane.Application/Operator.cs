// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;

namespace Homeport.Platform.ControlPlane.Application;

/// <summary>The operator who makes a change, as the caller token names them (ADR-0020).</summary>
/// <param name="Subject">The operator's subject at the identity provider.</param>
/// <param name="Name">The operator's display name.</param>
public sealed record Operator(string Subject, string Name)
{
    /// <summary>The audit entry of a change the operator made at <paramref name="now"/>.</summary>
    /// <param name="now">When the change was made.</param>
    /// <param name="action">What was done, such as <see cref="AuditEntry.TenantCreated"/>.</param>
    /// <param name="target">What it was done to, such as <see cref="AuditEntry.TenantTarget"/>.</param>
    /// <param name="summary">The change in one English sentence.</param>
    public AuditEntry Audit(DateTimeOffset now, string action, string target, string summary)
    {
        return new AuditEntry
        {
            Id = Guid.CreateVersion7(now),
            OccurredAt = now,
            OperatorSubject = Subject,
            OperatorName = Name,
            Action = action,
            Target = target,
            Summary = summary,
        };
    }
}
