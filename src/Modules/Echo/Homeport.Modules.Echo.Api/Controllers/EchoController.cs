// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Homeport.Modules.Echo.Api.Controllers;

/// <summary>Returns whatever it is sent.</summary>
/// <remarks>
/// Anonymous until the module validates the gateway's token, which comes
/// with #47 ("issue module tokens and check scopes"); it then requires
/// <see cref="EchoManifest.InvokeScope"/>.
/// </remarks>
[ApiController]
public sealed class EchoController : ControllerBase
{
    /// <summary>Returns the request body unchanged, with its content type.</summary>
    [HttpPost(EchoManifest.EchoPath)]
    [AllowAnonymous]
    [EndpointSummary("Returns the request body unchanged.")]
    public IActionResult Echo()
    {
        return File(Request.Body, Request.ContentType ?? MediaTypeNames.Application.Octet);
    }

    /// <summary>
    /// Returns the headers of the request as Echo received it, so a caller sees
    /// what the gateway forwards.
    /// </summary>
    /// <remarks>
    /// <c>Authorization</c> is left out: once the gateway signs module tokens,
    /// it carries one, and a token is never sent back.
    /// </remarks>
    [HttpGet(EchoManifest.RequestPath)]
    [AllowAnonymous]
    [EndpointSummary("Returns the headers of the request as Echo received it.")]
    public ActionResult<ReceivedRequest> Received()
    {
        var headers = Request.Headers
            .Where(header => !string.Equals(header.Key, HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(header => header.Key.ToLowerInvariant(), header => header.Value.ToString());

        return new ReceivedRequest(headers);
    }
}
