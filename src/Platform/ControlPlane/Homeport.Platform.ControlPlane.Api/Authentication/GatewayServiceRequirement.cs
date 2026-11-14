// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;

namespace Homeport.Platform.ControlPlane.Api.Authentication;

/// <summary>
/// The caller is the gateway itself: its service token, with the subject
/// <c>homeport-gateway</c>, no tenant and no scope (ADR-0014). A caller token,
/// the operator's included, fails it.
/// </summary>
internal sealed class GatewayServiceRequirement : IAuthorizationRequirement
{
    /// <summary>The policy that requires it.</summary>
    internal const string Policy = "gateway-service";
}
