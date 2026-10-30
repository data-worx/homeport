// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Gateway.Api.Errors;

/// <summary>
/// The problem types the gateway answers with itself. Each one has a reference
/// page under <c>docs/reference/problems/</c>.
/// </summary>
internal static class GatewayProblems
{
    /// <summary>No module is mounted at the path.</summary>
    internal static readonly GatewayProblem UnknownModule = new(
        "unknown-module",
        "No module is mounted at this path.",
        StatusCodes.Status404NotFound);

    /// <summary>The module failed in a way the caller cannot influence; its body is replaced.</summary>
    internal static readonly GatewayProblem Internal = new(
        "internal",
        "An internal error occurred.",
        StatusCodes.Status500InternalServerError);

    /// <summary>The gateway could not reach the module, or it broke off its answer.</summary>
    internal static readonly GatewayProblem ModuleUnreachable = new(
        "module-unreachable",
        "The module could not be reached.",
        StatusCodes.Status502BadGateway);

    /// <summary>No instance of the module is available.</summary>
    internal static readonly GatewayProblem ModuleUnavailable = new(
        "module-unavailable",
        "The module is not available.",
        StatusCodes.Status503ServiceUnavailable);

    /// <summary>The module did not answer in time.</summary>
    internal static readonly GatewayProblem ModuleTimeout = new(
        "module-timeout",
        "The module did not answer in time.",
        StatusCodes.Status504GatewayTimeout);
}

/// <summary>A problem type of the gateway.</summary>
/// <param name="Name">The last segment of the type URI.</param>
/// <param name="Title">A short English summary that is the same for every occurrence.</param>
/// <param name="Status">The HTTP status.</param>
internal sealed record GatewayProblem(string Name, string Title, int Status)
{
    /// <summary>The absolute URI that identifies the type.</summary>
    internal string Type => ProblemConventions.TypePrefix + Name;
}
