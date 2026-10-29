// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Sdk;

/// <summary>Turns a <see cref="Result{TValue}"/> into the answer of a controller action.</summary>
public static class ResultExtensions
{
    /// <summary>
    /// <c>200 OK</c> with the value of a success; for a failure, the problem type
    /// <paramref name="problemFor"/> maps its error onto, with the error's message
    /// as <c>detail</c>.
    /// </summary>
    /// <param name="result">The outcome of the use case.</param>
    /// <param name="problemFor">The module's mapping from a business error onto one of its problem types.</param>
    public static ActionResult<TValue> ToActionResult<TValue>(this Result<TValue> result, Func<Error, ProblemType> problemFor)
    {
        ArgumentNullException.ThrowIfNull(problemFor);

        if (result.IsSuccess)
        {
            return new OkObjectResult(result.Value);
        }

        return problemFor(result.Error).ToResult(result.Error.Message);
    }
}
