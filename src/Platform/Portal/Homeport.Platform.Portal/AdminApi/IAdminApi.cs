// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Platform.ControlPlane.V1;

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>
/// The admin API behind the gateway's admin endpoint (ADR-0020), called as
/// the signed-in operator.
/// </summary>
public interface IAdminApi
{
    /// <summary>Every tenant, ordered by name.</summary>
    Task<AdminResult<TenantsResponse>> GetTenantsAsync(CancellationToken cancellationToken);

    /// <summary>Creates a tenant.</summary>
    Task<AdminResult<TenantResponse>> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken);

    /// <summary>Renames the tenant <paramref name="tenantId"/>.</summary>
    Task<AdminResult<TenantResponse>> RenameTenantAsync(Guid tenantId, RenameTenantRequest request, CancellationToken cancellationToken);
}
