// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>
/// The clients of the admin API (ADR-0020): <c>/platform/v1/admin/clients</c>
/// on the gateway's admin endpoint, for the operator only. A client is created
/// at the identity provider; here its client id is mapped to a tenant and a plan.
/// </summary>
public static class ClientsApi
{
    /// <summary>
    /// The path below <c>/platform</c>, the path the control plane serves it at.
    /// <c>GET</c> returns <see cref="ClientsResponse"/>, of one tenant with the
    /// query parameter <see cref="TenantIdQuery"/>; <c>POST</c> takes
    /// <see cref="RegisterClientRequest"/> and returns the new <see cref="ClientResponse"/>.
    /// </summary>
    public const string Path = "/v1/admin/clients";

    /// <summary>The query parameter of <see cref="Path"/> that lists one tenant's clients only.</summary>
    public const string TenantIdQuery = "tenantId";

    /// <summary>The route value that names the client in <see cref="ClientPath"/>.</summary>
    public const string ClientIdRouteValue = "clientId";

    /// <summary>
    /// One client, its client id escaped as a path segment. <c>GET</c> returns
    /// its <see cref="ClientResponse"/>; <c>PUT</c> takes
    /// <see cref="ChangeClientPlanRequest"/> and returns it changed;
    /// <c>DELETE</c> removes it.
    /// </summary>
    public const string ClientPath = Path + "/{" + ClientIdRouteValue + "}";

    /// <summary>The field path of the client id in a validation problem.</summary>
    public const string ClientIdField = "clientId";

    /// <summary>The field path of the client's tenant in a validation problem.</summary>
    public const string TenantIdField = "tenantId";

    /// <summary>The field path of the client's plan in a validation problem.</summary>
    public const string PlanIdField = "planId";
}
