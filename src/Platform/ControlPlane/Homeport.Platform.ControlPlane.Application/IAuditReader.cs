// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;

namespace Homeport.Platform.ControlPlane.Application;

/// <summary>
/// Reads the control plane's audit log for the operator (ADR-0020). The
/// entries are platform data, not a tenant's, so reading them is not audited.
/// </summary>
public interface IAuditReader
{
    /// <summary>The page of entries <paramref name="query"/> selects, newest first.</summary>
    Task<AuditPage> ReadAsync(AuditQuery query, CancellationToken cancellationToken);
}
