// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Homeport.Shared.Authentication;

namespace Homeport.Shared.Internal.Authorization.Tests.Unit;

/// <summary>The users the gateway's tokens authenticate as.</summary>
internal static class Tokens
{
    internal const string Operator = "operator-subject";

    internal static ClaimsPrincipal Anonymous { get; } = new(new ClaimsIdentity());

    internal static ClaimsPrincipal With(params (string Type, string Value)[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Type, claim.Value)),
            authenticationType: "gateway"));
    }

    internal static ClaimsPrincipal OperatorToken() =>
        With((GatewayTokens.SubjectClaim, Operator), (GatewayTokens.ScopeClaim, GatewayTokens.AdminScope));

    internal static ClaimsPrincipal ServiceToken() =>
        With((GatewayTokens.SubjectClaim, GatewayTokens.GatewaySubject));
}
