// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Homeport.Shared.Client.Problems;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Metering.Api.Errors;

/// <summary>
/// Shapes every error body metering writes: its own problem type where one
/// applies, and <c>traceId</c>.
/// </summary>
internal sealed class MeteringProblemDetails : IConfigureOptions<ProblemDetailsOptions>
{
    private static readonly MeteringProblem[] ByStatus =
    [
        MeteringProblems.Unauthorized,
        MeteringProblems.InsufficientScope,
        MeteringProblems.Validation,
        MeteringProblems.Internal,
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
