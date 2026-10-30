// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Shared.Client.Modules.Echo.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
    // The operation ids of the OpenAPI document. A client generator names its
    // methods after them, so they stay stable when an action is renamed.
    private const string EchoOperation = "echo";
    private const string GetRequestOperation = "getRequest";

    /// <summary>Returns the request body unchanged.</summary>
    /// <remarks>The response carries the content type of the request.</remarks>
    /// <response code="200">The request body.</response>
    /// <response code="400">The request has no body.</response>
    [HttpPost(EchoManifest.EchoPath, Name = EchoOperation)]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    public IActionResult Echo()
    {
        // No request or response type, an exception to dotnet-core.web: the
        // body is returned unchanged, whatever its content type, so it has no
        // shape to describe.

        // A body announced by neither Content-Length nor chunked transfer is absent.
        if (HttpContext.Features.Get<IHttpRequestBodyDetectionFeature>()?.CanHaveBody != true)
        {
            return EchoProblems.EmptyBody.ToResult("POST a body; Echo returns it unchanged.");
        }

        return File(Request.Body, Request.ContentType ?? MediaTypeNames.Application.Octet);
    }

    /// <summary>Returns the headers of the request as Echo received it.</summary>
    /// <remarks>
    /// A caller sees what the gateway forwards. <c>Authorization</c> is left
    /// out: once the gateway signs module tokens, it carries one, and a token
    /// is never sent back.
    /// </remarks>
    /// <response code="200">The headers Echo received.</response>
    [HttpGet(EchoManifest.RequestPath, Name = GetRequestOperation)]
    [AllowAnonymous]
    [ProducesResponseType<ReceivedRequestResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    public ActionResult<ReceivedRequestResponse> Received()
    {
        var headers = Request.Headers
            .Where(header => !string.Equals(header.Key, HeaderNames.Authorization, StringComparison.OrdinalIgnoreCase))
            .ToDictionary(header => header.Key.ToLowerInvariant(), header => header.Value.ToString());

        return new ReceivedRequestResponse(headers);
    }
}
