// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Modules.Echo.Api.Controllers;

/// <summary>Returns whatever it is sent.</summary>
[ApiController]
public sealed class EchoController : ControllerBase
{
    /// <summary>Returns the request body unchanged, with its content type.</summary>
    /// <remarks>
    /// Anonymous until the module validates the gateway's token, which comes
    /// with #47 ("issue module tokens and check scopes"); it then requires
    /// <see cref="EchoManifest.InvokeScope"/>.
    /// </remarks>
    [HttpPost(EchoManifest.EchoPath)]
    [AllowAnonymous]
    [EndpointSummary("Returns the request body unchanged.")]
    public IActionResult Echo()
    {
        return File(Request.Body, Request.ContentType ?? MediaTypeNames.Application.Octet);
    }
}
