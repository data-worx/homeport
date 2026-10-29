// SPDX-License-Identifier: AGPL-3.0-or-later

using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using Homeport.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Sdk.Tests.Integration.Controllers;

/// <summary>Endpoints that answer every way the SDK turns into an error response.</summary>
[ApiController]
[AllowAnonymous]
public sealed class ThingsController : ControllerBase
{
    internal const string ThrowPath = "/v1/things/throw";
    internal const string ExceptionMessage = "Connection string Host=secret-db failed";
    internal const string ConflictPath = "/v1/things/conflict";
    internal const string ConflictDetail = "A thing with that name exists.";
    internal const string CreatePath = "/v1/things";
    internal const string LookupPath = "/v1/things/";
    internal const string MissingName = "missing";
    internal const string ThrowFailureDescription = "The thing always fails.";

    [HttpGet(ThrowPath)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        MediaTypeNames.Application.ProblemJson,
        Description = ThrowFailureDescription)]
    public IActionResult Throw()
    {
        throw new InvalidOperationException(ExceptionMessage);
    }

    [HttpGet(ConflictPath)]
    public IActionResult Clash()
    {
        return TestProblems.Conflict.ToResult(ConflictDetail);
    }

    [HttpPost(CreatePath)]
    public ActionResult<ThingRequest> Create(ThingRequest request)
    {
        return request;
    }

    [HttpGet(LookupPath + "{name}")]
    public ActionResult<string> Lookup(string name)
    {
        var result = name == MissingName
            ? Result<string>.Failure(new Error(TestProblems.NotFoundCode, $"No thing is named '{name}'."))
            : Result<string>.Success(name);

        return result.ToActionResult(TestProblems.For);
    }
}

/// <summary>A thing to create, with nested items so field paths have depth.</summary>
public sealed record ThingRequest([Required] string? Name, [Required] IReadOnlyList<ThingItem>? Items);

/// <summary>One item of a thing.</summary>
public sealed record ThingItem([Required] string? Name);
