// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.Gateway.Api.ApiDocs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Controllers;

/// <summary>The documents of the consolidated API docs (ADR-0017).</summary>
/// <remarks>
/// Anonymous on purpose: ADR-0017 makes the docs anonymous while they are on,
/// and off they answer <c>404</c>.
/// </remarks>
[ApiController]
[AllowAnonymous]
[Route(ApiDocsPaths.Documents)]
public sealed class ApiDocsController(ApiDocsCatalog catalog, IOptions<ApiDocsOptions> options) : ControllerBase
{
    /// <summary>One document: a module's, under its module id, or the platform's, under <c>platform</c>.</summary>
    /// <remarks>
    /// The document is the body itself, as OpenAPI defines it, so it has no
    /// response type of its own.
    /// </remarks>
    /// <param name="documentName">The module id, or <c>platform</c>.</param>
    /// <response code="200">The OpenAPI document.</response>
    /// <response code="404">The docs are off, or there is no such document.</response>
    [HttpGet("{" + ApiDocsPaths.DocumentName + "}.json")]
    [ProducesResponseType<string>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public IActionResult Get(string documentName)
    {
        var document = options.Value.Enabled ? catalog.Find(documentName) : null;

        return document is null ? NotFound() : Content(document, MediaTypeNames.Application.Json);
    }
}
