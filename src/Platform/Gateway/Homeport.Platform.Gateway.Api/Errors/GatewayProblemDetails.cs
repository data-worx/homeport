// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Shared.Contracts;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

namespace Homeport.Platform.Gateway.Api.Errors;

/// <summary>
/// The one place that shapes every error body the gateway writes, whether the
/// error is its own or a module's body it replaced: it picks the gateway's
/// problem type where one applies and adds <c>traceId</c>.
/// </summary>
/// <remarks>
/// Every writer goes through <see cref="IProblemDetailsService"/>: the status
/// code pages for an error status without a body, the exception handler for an
/// unhandled exception. An error that needs no type of its own keeps ASP.NET's
/// default, the RFC 9110 section of its status.
/// </remarks>
internal sealed class GatewayProblemDetails(ILogger<GatewayProblemDetails> logger)
    : IConfigureOptions<ProblemDetailsOptions>
{
    public void Configure(ProblemDetailsOptions options)
    {
        options.CustomizeProblemDetails = Customize;
    }

    /// <summary>The W3C trace id of the request, so a caller can look the trace up by it.</summary>
    internal static string TraceId(HttpContext context)
    {
        return Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier;
    }

    private void Customize(ProblemDetailsContext context)
    {
        var details = context.ProblemDetails;
        details.Extensions[ProblemConventions.TraceIdExtension] = TraceId(context.HttpContext);

        if (Match(context.HttpContext, details.Status) is { } problem)
        {
            details.Type = problem.Type;
            details.Title = problem.Title;
        }
    }

    private GatewayProblem? Match(HttpContext context, int? status)
    {
        if (context.Features.Get<IForwarderErrorFeature>() is { } failure)
        {
            // Logged here, where the failure is answered.
            logger.LogWarning(
                "Forwarding to module {ModuleId} failed with {ForwarderError}; answered {StatusCode}",
                context.Features.Get<IReverseProxyFeature>()?.Cluster.Config.ClusterId,
                failure.Error,
                status);

            return status switch
            {
                StatusCodes.Status503ServiceUnavailable => GatewayProblems.ModuleUnavailable,
                StatusCodes.Status504GatewayTimeout => GatewayProblems.ModuleTimeout,
                _ => GatewayProblems.ModuleUnreachable,
            };
        }

        // No endpoint means no route matched: nothing is mounted at the path.
        if (status == StatusCodes.Status404NotFound
            && context.GetEndpoint() is null
            && context.Request.Path.StartsWithSegments(StaticRoutes.ModulesPrefix))
        {
            return GatewayProblems.UnknownModule;
        }

        return status == StatusCodes.Status500InternalServerError ? GatewayProblems.Internal : null;
    }
}
