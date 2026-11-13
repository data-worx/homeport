// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>
/// Issues the token the gateway sends a module with every forwarded request
/// (ADR-0011): audience the module id, claims tenant, client, subject and
/// scopes. The caller token for a platform service (ADR-0019) is the same
/// token for the service's audience; the operator's caller token for an
/// admin API (ADR-0020) carries the operator instead of a client. A token is
/// issued per request, since its claims are the caller's.
/// </summary>
public sealed class ModuleTokenIssuer(SigningKeys keys, IOptions<ModuleTokenOptions> options, TimeProvider time)
{
    private readonly JsonWebTokenHandler handler = new();

    /// <summary>A token that tells <see cref="ModuleCaller.Audience"/> who calls it.</summary>
    public string Issue(ModuleCaller caller)
    {
        ArgumentNullException.ThrowIfNull(caller);

        return Issue(caller.Audience, new Dictionary<string, object>
        {
            [GatewayTokens.SubjectClaim] = caller.Subject,
            [GatewayTokens.TenantIdClaim] = caller.TenantId.ToString(),
            [GatewayTokens.ClientIdClaim] = caller.ClientId,
            [GatewayTokens.ScopeClaim] = string.Join(GatewayTokens.ScopeSeparator, caller.Scopes),
        });
    }

    /// <summary>
    /// A caller token that tells <see cref="OperatorCaller.Audience"/> the
    /// operator calls its admin API (ADR-0020): the operator's subject and
    /// display name, the one scope <see cref="GatewayTokens.AdminScope"/>, and
    /// no tenant or client.
    /// </summary>
    public string Issue(OperatorCaller caller)
    {
        ArgumentNullException.ThrowIfNull(caller);

        return Issue(caller.Audience, new Dictionary<string, object>
        {
            [GatewayTokens.SubjectClaim] = caller.Subject,
            [GatewayTokens.NameClaim] = caller.Name,
            [GatewayTokens.ScopeClaim] = GatewayTokens.AdminScope,
        });
    }

    private string Issue(string audience, Dictionary<string, object> claims)
    {
        var now = time.GetUtcNow();

        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = GatewayTokens.Issuer,
            Audience = audience,
            Claims = claims,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = (now + options.Value.Lifetime).UtcDateTime,
            SigningCredentials = keys.Credentials,
        });
    }
}
