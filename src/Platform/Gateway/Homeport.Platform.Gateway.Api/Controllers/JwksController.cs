// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.Signing;
using Homeport.Shared.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Platform.Gateway.Api.Controllers;

/// <summary>
/// The gateway's JWKS: the public keys modules and platform services validate
/// its tokens against (ADR-0011, ADR-0014).
/// </summary>
/// <remarks>
/// Anonymous on purpose: it serves public keys only, and a validator must be
/// able to read them before it can authenticate anything. No private key
/// material can reach it, as <see cref="PublicJsonWebKey"/> holds the public
/// point alone.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route(GatewayTokens.JwksPath)]
public sealed class JwksController(SigningKeys keys) : ControllerBase
{
    /// <summary>The current signing key and, when configured, the additional public key.</summary>
    [HttpGet]
    public JsonWebKeySetResponse Get()
    {
        return keys.Jwks;
    }
}
