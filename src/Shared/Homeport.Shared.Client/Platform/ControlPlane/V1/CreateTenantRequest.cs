// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>A new tenant.</summary>
/// <param name="Name">Its name: required, unique among tenants, compared case-sensitively.</param>
public sealed record CreateTenantRequest(string Name);
