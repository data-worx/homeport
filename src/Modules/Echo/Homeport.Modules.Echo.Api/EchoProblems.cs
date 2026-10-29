// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Sdk;

namespace Homeport.Modules.Echo.Api;

/// <summary>The problem types Echo answers with, under a prefix of its own.</summary>
internal static class EchoProblems
{
    private const string TypePrefix = "https://homeport.data-worx.de/modules/echo/problems/";

    /// <summary>The request carries no body, so there is nothing to echo.</summary>
    internal static readonly ProblemType EmptyBody = new(
        new Uri(TypePrefix + "empty-body"),
        "The request has no body to echo.",
        StatusCodes.Status400BadRequest);
}
