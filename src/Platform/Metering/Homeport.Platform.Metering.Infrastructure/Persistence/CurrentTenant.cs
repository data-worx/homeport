// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// The tenant a scope works for. The query filters of
/// <see cref="MeteringDbContext"/> and the session setting of
/// <see cref="TenantSessionInterceptor"/> both read it, so the two layers of
/// isolation (ADR-0005) never disagree.
/// </summary>
internal sealed class CurrentTenant
{
    /// <summary>The tenant; none until it is set, and then no tenant-owned row is visible.</summary>
    internal Guid? Id { get; set; }
}
