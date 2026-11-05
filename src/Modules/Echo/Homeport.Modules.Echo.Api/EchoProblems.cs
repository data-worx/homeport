// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.Echo.Domain.Batches;
using Homeport.Sdk;
using Homeport.Shared.Contracts;

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

    /// <summary>The batch holds no item, so there is nothing to echo or to count.</summary>
    internal static readonly ProblemType EmptyBatch = new(
        new Uri(TypePrefix + "empty-batch"),
        "The batch has no item to echo.",
        StatusCodes.Status400BadRequest);

    /// <summary>The problem type a business error of Echo is answered with.</summary>
    internal static ProblemType For(Error error) => error.Code switch
    {
        BatchRules.EmptyCode => EmptyBatch,
        _ => throw new ArgumentOutOfRangeException(nameof(error), error.Code, "Echo maps every error it returns."),
    };
}
