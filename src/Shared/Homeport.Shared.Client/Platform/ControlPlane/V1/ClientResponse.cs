// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>A registered client as the operator sees it.</summary>
/// <param name="ClientId">The client id as the identity provider issued it.</param>
/// <param name="TenantId">The tenant the client belongs to, for good.</param>
/// <param name="TenantName">The tenant's name.</param>
/// <param name="PlanId">The plan the client's requests are held to.</param>
/// <param name="PlanName">The plan's name.</param>
public sealed record ClientResponse(string ClientId, Guid TenantId, string TenantName, Guid PlanId, string PlanName);
