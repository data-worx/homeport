// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Application.Clients;

/// <summary>A registered client with the names of its tenant and plan.</summary>
/// <param name="ClientId">The client id as the identity provider issued it.</param>
/// <param name="TenantId">The tenant the client belongs to.</param>
/// <param name="TenantName">The tenant's name.</param>
/// <param name="PlanId">The plan the client is held to.</param>
/// <param name="PlanName">The plan's name.</param>
public sealed record ClientOverview(string ClientId, Guid TenantId, string TenantName, Guid PlanId, string PlanName);
