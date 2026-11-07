// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;

namespace Homeport.Platform.Metering.Api.Authentication;

/// <summary>
/// The caller is a client the gateway forwarded, with a tenant and the scope
/// <c>usage.read</c> (ADR-0019). A service token names no tenant and fails it.
/// </summary>
internal sealed class UsageReaderRequirement : IAuthorizationRequirement
{
    /// <summary>The policy that requires it.</summary>
    internal const string Policy = "usage-reader";
}
