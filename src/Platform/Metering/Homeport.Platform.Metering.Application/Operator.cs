// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain;

namespace Homeport.Platform.Metering.Application;

/// <summary>The operator who reads usage, as the caller token names them (ADR-0020).</summary>
/// <param name="Subject">The operator's subject at the identity provider.</param>
/// <param name="Name">The operator's display name.</param>
public sealed record Operator(string Subject, string Name)
{
    /// <summary>The audit entry of a read the operator made at <paramref name="now"/>.</summary>
    /// <param name="now">When the read was made.</param>
    /// <param name="action">What was done, such as <see cref="AuditEntry.UsageRead"/>.</param>
    /// <param name="target">Whose data was read, such as <see cref="AuditEntry.AllTenantsTarget"/>.</param>
    /// <param name="summary">The read in one English sentence.</param>
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
