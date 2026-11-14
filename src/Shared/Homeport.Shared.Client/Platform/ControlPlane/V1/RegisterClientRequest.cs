// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>A client of the identity provider, to be mapped to a tenant and a plan.</summary>
/// <param name="ClientId">The client id exactly as the identity provider issued it: required and not yet registered.</param>
/// <param name="TenantId">The tenant the client belongs to: an existing tenant. It cannot be changed later.</param>
/// <param name="PlanId">The plan the client's requests are held to: an existing plan.</param>
public sealed record RegisterClientRequest(string ClientId, Guid TenantId, Guid PlanId);
