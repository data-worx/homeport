// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Sdk.Tests.Integration.Controllers;

/// <summary>Endpoints that need a module token, as a module that marks none of them anonymous.</summary>
[ApiController]
public sealed class CallerController : ControllerBase
{
    internal const string CallerPath = "/v1/caller";
    internal const string ScopedPath = "/v1/caller/scoped";

    [HttpGet(CallerPath)]
    public ActionResult<HomeportCaller> Caller()
    {
        return HomeportCaller.From(User);
    }

    [HttpGet(ScopedPath)]
    [Authorize(Policy = TestModule.WriteScope)]
    public IActionResult Scoped()
    {
        return Ok();
    }
}
