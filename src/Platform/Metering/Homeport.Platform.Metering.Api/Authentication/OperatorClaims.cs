// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Homeport.Platform.Metering.Application;
using Homeport.Shared.Authentication;

namespace Homeport.Platform.Metering.Api.Authentication;

/// <summary>The operator an admin request comes from, read from the caller token.</summary>
internal static class OperatorClaims
{
    /// <summary>
    /// The operator a caller token names, for the audit entry. Without a
    /// display name, the subject stands in for it.
    /// </summary>
    internal static Operator OperatorOf(ClaimsPrincipal user)
    {
        // The admin policy has made sure the token names one.
        var subject = user.FindFirst(GatewayTokens.SubjectClaim)?.Value
            ?? throw new InvalidOperationException("An admin request reached the controller without a subject.");

        return new Operator(subject, user.FindFirst(GatewayTokens.NameClaim)?.Value ?? subject);
    }
}
