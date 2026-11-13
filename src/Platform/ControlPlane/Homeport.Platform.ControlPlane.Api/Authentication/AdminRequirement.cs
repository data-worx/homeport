// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;

namespace Homeport.Platform.ControlPlane.Api.Authentication;

/// <summary>
/// The caller is the operator: a caller token with the scope <c>admin</c>, a
/// subject and no tenant (ADR-0020). A service token, or a client's caller
/// token, fails it.
/// </summary>
internal sealed class AdminRequirement : IAuthorizationRequirement
{
    /// <summary>The policy that requires it.</summary>
    internal const string Policy = "admin";
}
