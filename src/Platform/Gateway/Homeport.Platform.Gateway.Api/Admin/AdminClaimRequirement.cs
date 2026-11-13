// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;

namespace Homeport.Platform.Gateway.Api.Admin;

/// <summary>
/// The operator's token carries a subject and the claim that grants admin
/// access, as <see cref="AdminApiOptions"/> configures it (ADR-0020).
/// </summary>
internal sealed class AdminClaimRequirement : IAuthorizationRequirement;
