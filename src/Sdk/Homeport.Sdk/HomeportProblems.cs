// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Problems;
using Microsoft.AspNetCore.Http;

namespace Homeport.Sdk;

/// <summary>
/// Homeport's own problem types that the SDK answers with. Each one has a
/// reference page under <c>docs/reference/problems/</c>.
/// </summary>
public static class HomeportProblems
{
    /// <summary>An unexpected failure inside the module; the body says nothing more.</summary>
    public static readonly ProblemType Internal = new(
        new Uri(ProblemConventions.TypePrefix + "internal"),
        "An internal error occurred.",
        StatusCodes.Status500InternalServerError);

    /// <summary>The request failed validation; <see cref="ProblemConventions.ErrorsExtension"/> says where.</summary>
    public static readonly ProblemType Validation = new(
        new Uri(ProblemConventions.TypePrefix + "validation"),
        "One or more validation errors occurred.",
        StatusCodes.Status400BadRequest);
}
