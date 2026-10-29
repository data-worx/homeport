// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Sdk;

/// <summary>
/// Answers with a <see cref="ProblemType"/>. The body is built when the result
/// executes, through the <see cref="ProblemDetailsFactory"/>, so it carries the
/// same extensions as every other problem the module answers with.
/// </summary>
internal sealed class ProblemTypeResult(ProblemType problemType, string detail) : ActionResult
{
    public override Task ExecuteResultAsync(ActionContext context)
    {
        var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
        var problem = factory.CreateProblemDetails(
            context.HttpContext,
            problemType.Status,
            problemType.Title,
            problemType.Type.AbsoluteUri,
            detail);

        return new ObjectResult(problem) { StatusCode = problemType.Status }.ExecuteResultAsync(context);
    }
}
