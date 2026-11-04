// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>The client's token grants the scope the matched route declares in the manifest.</summary>
internal sealed class RouteScopeRequirement : IAuthorizationRequirement;
