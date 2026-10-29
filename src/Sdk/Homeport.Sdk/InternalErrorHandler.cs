// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Homeport.Sdk;

/// <summary>
/// Answers an unhandled exception with <see cref="HomeportProblems.Internal"/>
/// and nothing of the exception, and logs it once, here, where it is handled.
/// </summary>
internal sealed class InternalErrorHandler(IProblemDetailsService problemDetails, ILogger<InternalErrorHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        // The endpoint names the action; the path is left out, since it can carry
        // personal data. The middleware has cleared the endpoint from the context
        // by now, so it is read from the feature that kept it.
        var endpoint = httpContext.Features.Get<IExceptionHandlerFeature>()?.Endpoint;
        logger.LogError(exception, "Unhandled exception in {Endpoint}", endpoint?.DisplayName);

        var problem = HomeportProblems.Internal;
        httpContext.Response.StatusCode = problem.Status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Type = problem.Type.AbsoluteUri,
                Title = problem.Title,
                Status = problem.Status,
            },
        });
    }
}
