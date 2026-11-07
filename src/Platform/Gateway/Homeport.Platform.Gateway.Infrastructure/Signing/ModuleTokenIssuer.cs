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
/// token for the service's audience. A token is issued per request, since
/// its claims are the caller's.
/// </summary>
public sealed class ModuleTokenIssuer(SigningKeys keys, IOptions<ModuleTokenOptions> options, TimeProvider time)
{
    private readonly JsonWebTokenHandler handler = new();

    /// <summary>A token that tells <see cref="ModuleCaller.Audience"/> who calls it.</summary>
    public string Issue(ModuleCaller caller)
    {
        ArgumentNullException.ThrowIfNull(caller);

        var now = time.GetUtcNow();

        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = GatewayTokens.Issuer,
            Audience = caller.Audience,
            Claims = new Dictionary<string, object>
            {
                [GatewayTokens.SubjectClaim] = caller.Subject,
                [GatewayTokens.TenantIdClaim] = caller.TenantId.ToString(),
                [GatewayTokens.ClientIdClaim] = caller.ClientId,
                [GatewayTokens.ScopeClaim] = string.Join(GatewayTokens.ScopeSeparator, caller.Scopes),
            },
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = (now + options.Value.Lifetime).UtcDateTime,
            SigningCredentials = keys.Credentials,
        });
    }
}
