// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>A tenant's new name.</summary>
/// <param name="Name">The name: required, unique among tenants, compared case-sensitively.</param>
public sealed record RenameTenantRequest(string Name);
