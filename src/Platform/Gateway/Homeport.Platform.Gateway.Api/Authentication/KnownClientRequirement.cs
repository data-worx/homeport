// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>The client named in the token is known to the control plane.</summary>
internal sealed class KnownClientRequirement : IAuthorizationRequirement;
