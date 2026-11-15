// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Platform.Registry.V1;

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

    /// <summary>Every plan, ordered by name.</summary>
    Task<AdminResult<PlansResponse>> GetPlansAsync(CancellationToken cancellationToken);

    /// <summary>Creates a plan.</summary>
    Task<AdminResult<PlanResponse>> CreatePlanAsync(CreatePlanRequest request, CancellationToken cancellationToken);

    /// <summary>Changes the name and limits of the plan <paramref name="planId"/>.</summary>
    Task<AdminResult<PlanResponse>> ChangePlanAsync(Guid planId, ChangePlanRequest request, CancellationToken cancellationToken);

    /// <summary>Deletes the plan <paramref name="planId"/>; on success the value is its id.</summary>
    Task<AdminResult<Guid>> DeletePlanAsync(Guid planId, CancellationToken cancellationToken);

    /// <summary>Every registered client, or every client of <paramref name="tenantId"/>, ordered by client id.</summary>
    Task<AdminResult<ClientsResponse>> GetClientsAsync(Guid? tenantId, CancellationToken cancellationToken);

    /// <summary>Registers a client of the identity provider for a tenant and a plan.</summary>
    Task<AdminResult<ClientResponse>> RegisterClientAsync(RegisterClientRequest request, CancellationToken cancellationToken);

    /// <summary>Holds the client <paramref name="clientId"/> to another plan.</summary>
    Task<AdminResult<ClientResponse>> ChangeClientPlanAsync(string clientId, ChangeClientPlanRequest request, CancellationToken cancellationToken);

    /// <summary>Removes the client <paramref name="clientId"/>; on success the value is its client id.</summary>
    Task<AdminResult<string>> RemoveClientAsync(string clientId, CancellationToken cancellationToken);

    /// <summary>Every module with at least one live instance, ordered by module id.</summary>
    Task<AdminResult<ModulesResponse>> GetModulesAsync(CancellationToken cancellationToken);
}
