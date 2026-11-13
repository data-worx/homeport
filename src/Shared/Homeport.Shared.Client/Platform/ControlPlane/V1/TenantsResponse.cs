// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>Every tenant of the installation.</summary>
/// <param name="Tenants">The tenants, ordered by name.</param>
public sealed record TenantsResponse(IReadOnlyList<TenantResponse> Tenants);
