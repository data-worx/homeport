// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Gateway.Api.Errors;

/// <summary>
/// The problem types the gateway answers with itself. Each one has a reference
/// page under <c>docs/reference/problems/</c>.
/// </summary>
internal static class GatewayProblems
{
    /// <summary>The request carries no valid client token.</summary>
    internal static readonly GatewayProblem Unauthorized = new(
        "unauthorized",
        "The request is not authenticated.",
        StatusCodes.Status401Unauthorized);

    /// <summary>The client is authenticated, but the operator has not mapped it to a tenant.</summary>
    internal static readonly GatewayProblem UnknownClient = new(
        "unknown-client",
        "The client is not known to Homeport.",
        StatusCodes.Status403Forbidden);

    /// <summary>The client is known, but its token lacks the scope the route declares.</summary>
    internal static readonly GatewayProblem InsufficientScope = new(
        "insufficient-scope",
        "The client's token lacks the scope this route requires.",
        StatusCodes.Status403Forbidden);

    /// <summary>The control plane could not be asked about the client.</summary>
    internal static readonly GatewayProblem ControlPlaneUnavailable = new(
        "control-plane-unavailable",
        "The control plane is not available.",
        StatusCodes.Status503ServiceUnavailable);

    /// <summary>The tenant's requests exceeded the rate limit of its plan.</summary>
    internal static readonly GatewayProblem RateLimited = new(
        "rate-limited",
        "The rate limit is exceeded.",
        StatusCodes.Status429TooManyRequests);

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
