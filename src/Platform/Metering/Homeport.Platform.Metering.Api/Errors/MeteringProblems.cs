// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Metering.Api.Errors;

/// <summary>
/// The problem types metering answers with. Each one has a reference page
/// under <c>docs/reference/problems/</c>.
/// </summary>
internal static class MeteringProblems
{
    /// <summary>The request carries no valid gateway token for metering.</summary>
    internal static readonly MeteringProblem Unauthorized = new(
        "unauthorized",
        "The request is not authenticated.",
        StatusCodes.Status401Unauthorized);

    /// <summary>The token names no tenant, or lacks the scope the endpoint requires.</summary>
    internal static readonly MeteringProblem InsufficientScope = new(
        "insufficient-scope",
        "The client's token lacks the scope this route requires.",
        StatusCodes.Status403Forbidden);

    /// <summary>The request failed validation; <see cref="ProblemConventions.ErrorsExtension"/> says where.</summary>
    internal static readonly MeteringProblem Validation = new(
        "validation",
        "One or more validation errors occurred.",
        StatusCodes.Status400BadRequest);

    /// <summary>An unexpected failure inside metering.</summary>
    internal static readonly MeteringProblem Internal = new(
        "internal",
        "An internal error occurred.",
        StatusCodes.Status500InternalServerError);
}

/// <summary>A problem type of metering.</summary>
/// <param name="Name">The last segment of the type URI.</param>
/// <param name="Title">A short English summary that is the same for every occurrence.</param>
/// <param name="Status">The HTTP status.</param>
internal sealed record MeteringProblem(string Name, string Title, int Status)
{
    /// <summary>The absolute URI that identifies the type.</summary>
    internal string Type => ProblemConventions.TypePrefix + Name;
}
