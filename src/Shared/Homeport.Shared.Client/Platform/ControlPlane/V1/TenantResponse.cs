// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>A tenant as the operator sees it.</summary>
/// <param name="Id">The tenant's id, which every token, usage record and audit entry names.</param>
/// <param name="Name">The tenant's name, unique among tenants.</param>
/// <param name="ClientCount">How many clients the tenant has.</param>
public sealed record TenantResponse(Guid Id, string Name, int ClientCount);
