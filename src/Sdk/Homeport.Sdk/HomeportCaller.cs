// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Homeport.Shared.Authentication;

namespace Homeport.Sdk;

/// <summary>
/// Who calls the module, as the gateway's module token names it: the only
/// identity source a module may trust (ADR-0011).
/// </summary>
/// <param name="TenantId">The tenant the client acts for.</param>
/// <param name="ClientId">The client id as the identity provider issued it.</param>
/// <param name="Subject">The subject of the client's token.</param>
/// <param name="Scopes">The scopes the client's token grants.</param>
public sealed record HomeportCaller(Guid TenantId, string ClientId, string Subject, IReadOnlyList<string> Scopes)
{
    /// <summary>Whether the caller's token grants <paramref name="scope"/>.</summary>
    public bool HasScope(string scope) => Scopes.Contains(scope, StringComparer.Ordinal);

    /// <summary>
    /// The caller named by <paramref name="user"/>, the principal of a request
    /// authenticated with a module token.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The request is not authenticated with a module token, as on an endpoint
    /// that allows anonymous callers.
    /// </exception>
    public static HomeportCaller From(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Identity?.IsAuthenticated != true
            || !Guid.TryParse(user.FindFirst(GatewayTokens.TenantIdClaim)?.Value, out var tenantId)
            || user.FindFirst(GatewayTokens.ClientIdClaim)?.Value is not { Length: > 0 } clientId
            || user.FindFirst(GatewayTokens.SubjectClaim)?.Value is not { Length: > 0 } subject)
        {
            throw new InvalidOperationException("The request is not authenticated with a module token.");
        }

        var scopes = (user.FindFirst(GatewayTokens.ScopeClaim)?.Value ?? string.Empty)
            .Split(GatewayTokens.ScopeSeparator, StringSplitOptions.RemoveEmptyEntries);

        return new HomeportCaller(tenantId, clientId, subject, scopes);
    }
}
