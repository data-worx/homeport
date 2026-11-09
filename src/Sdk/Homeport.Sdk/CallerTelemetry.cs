// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Security.Claims;
using Homeport.Shared.Authentication;
using Homeport.Shared.ServiceDefaults;

namespace Homeport.Sdk;

/// <summary>
/// Tags a module request's span with the module and, for a request with a
/// module token, the tenant and client it names, so the module's span carries
/// the same attributes as the gateway's.
/// </summary>
internal static class CallerTelemetry
{
    /// <summary>Tags <paramref name="activity"/>; an anonymous caller gets the module id only.</summary>
    internal static void Tag(Activity? activity, string moduleId, ClaimsPrincipal user)
    {
        if (activity is null)
        {
            return;
        }

        activity.SetTag(TelemetryAttributes.ModuleId, moduleId);
        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        activity
            .SetTag(TelemetryAttributes.TenantId, user.FindFirst(GatewayTokens.TenantIdClaim)?.Value)
            .SetTag(TelemetryAttributes.ClientId, user.FindFirst(GatewayTokens.ClientIdClaim)?.Value);
    }
}
