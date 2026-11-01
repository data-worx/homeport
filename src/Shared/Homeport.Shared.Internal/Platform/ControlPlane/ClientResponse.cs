// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.ControlPlane;

/// <summary>Which tenant a client belongs to, and the limits of its plan.</summary>
/// <param name="ClientId">The client id as the identity provider issued it.</param>
/// <param name="TenantId">The tenant the client acts for.</param>
/// <param name="Plan">The limits the client's requests are held to.</param>
public sealed record ClientResponse(string ClientId, Guid TenantId, PlanResponse Plan);
