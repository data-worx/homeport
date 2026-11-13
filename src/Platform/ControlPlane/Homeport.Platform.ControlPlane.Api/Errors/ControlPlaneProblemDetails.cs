// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Homeport.Shared.Client.Problems;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.ControlPlane.Api.Errors;

/// <summary>
/// Shapes every error body the control plane writes: its own problem type
/// where one applies, and <c>traceId</c>.
/// </summary>
/// <remarks>
/// The only conflict the control plane answers is a plan in use, so the
/// status alone picks every type. An error that needs no type of its own,
/// such as an unknown client, tenant or plan, keeps
/// ASP.NET's default, the RFC 9110 section of its status.
/// </remarks>
internal sealed class ControlPlaneProblemDetails : IConfigureOptions<ProblemDetailsOptions>
{
    private static readonly ControlPlaneProblem[] ByStatus =
    [
        ControlPlaneProblems.Unauthorized,
        ControlPlaneProblems.InsufficientScope,
        ControlPlaneProblems.Validation,
        ControlPlaneProblems.PlanInUse,
        ControlPlaneProblems.Internal,
    ];

    public void Configure(ProblemDetailsOptions options)
    {
        options.CustomizeProblemDetails = Customize;
    }

    private static void Customize(ProblemDetailsContext context)
    {
        var details = context.ProblemDetails;
        details.Extensions[ProblemConventions.TraceIdExtension] =
            Activity.Current?.TraceId.ToHexString() ?? context.HttpContext.TraceIdentifier;

        if (ByStatus.FirstOrDefault(problem => problem.Status == details.Status) is { } match)
        {
            details.Type = match.Type;
            details.Title = match.Title;
        }
    }
}
