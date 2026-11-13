// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>
/// The tenants of the admin API (ADR-0020): <c>/platform/v1/admin/tenants</c>
/// on the gateway's admin endpoint, for the operator only.
/// </summary>
public static class TenantsApi
{
    /// <summary>
    /// The path below <c>/platform</c>, the path the control plane serves it at.
    /// <c>GET</c> returns <see cref="TenantsResponse"/>; <c>POST</c> takes
    /// <see cref="CreateTenantRequest"/> and returns the new <see cref="TenantResponse"/>.
    /// </summary>
    public const string Path = "/v1/admin/tenants";

    /// <summary>The route value that names the tenant in <see cref="TenantPath"/>.</summary>
    public const string TenantIdRouteValue = "tenantId";

    /// <summary>
    /// One tenant. <c>GET</c> returns its <see cref="TenantResponse"/>;
    /// <c>PUT</c> takes <see cref="RenameTenantRequest"/> and returns it renamed.
    /// </summary>
    public const string TenantPath = Path + "/{" + TenantIdRouteValue + "}";

    /// <summary>The field path of the tenant's name in a validation problem.</summary>
    public const string NameField = "name";
}
