// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.ControlPlane.Errors;

/// <summary>
/// The problem types the control plane answers with. Each one has a reference
/// page under <c>docs/reference/problems/</c>.
/// </summary>
internal static class ControlPlaneProblems
{
    /// <summary>The request carries no valid service token.</summary>
    internal static readonly ControlPlaneProblem Unauthorized = new(
        "unauthorized",
        "The request is not authenticated.",
        StatusCodes.Status401Unauthorized);

    /// <summary>An unexpected failure inside the control plane.</summary>
    internal static readonly ControlPlaneProblem Internal = new(
        "internal",
        "An internal error occurred.",
        StatusCodes.Status500InternalServerError);
}

/// <summary>A problem type of the control plane.</summary>
/// <param name="Name">The last segment of the type URI.</param>
/// <param name="Title">A short English summary that is the same for every occurrence.</param>
/// <param name="Status">The HTTP status.</param>
internal sealed record ControlPlaneProblem(string Name, string Title, int Status)
{
    /// <summary>The absolute URI that identifies the type.</summary>
    internal string Type => ProblemConventions.TypePrefix + Name;
}
