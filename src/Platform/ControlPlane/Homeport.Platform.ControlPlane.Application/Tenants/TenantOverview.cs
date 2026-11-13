// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.ControlPlane.Application.Tenants;

/// <summary>A tenant with what the operator sees about it at a glance.</summary>
/// <param name="Id">The tenant's id.</param>
/// <param name="Name">The tenant's name.</param>
/// <param name="ClientCount">How many clients the tenant has.</param>
public sealed record TenantOverview(Guid Id, string Name, int ClientCount);
