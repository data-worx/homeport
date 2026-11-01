// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Registry.Api.Errors;

/// <summary>
/// The problem types the registry answers with. Each one has a reference page
/// under <c>docs/reference/problems/</c>.
/// </summary>
internal static class RegistryProblems
{
    /// <summary>The request carries no valid registration key, or no valid service token.</summary>
    internal static readonly RegistryProblem Unauthorized = new(
        "unauthorized",
        "The request is not authenticated.",
        StatusCodes.Status401Unauthorized);

    /// <summary>The manifest breaks the module contract; <see cref="ProblemConventions.ErrorsExtension"/> says where.</summary>
    internal static readonly RegistryProblem InvalidManifest = new(
        "invalid-manifest",
        "The manifest breaks the module contract.",
        StatusCodes.Status422UnprocessableEntity);

    /// <summary>The request failed validation; <see cref="ProblemConventions.ErrorsExtension"/> says where.</summary>
    internal static readonly RegistryProblem Validation = new(
        "validation",
        "One or more validation errors occurred.",
        StatusCodes.Status400BadRequest);

    /// <summary>An unexpected failure inside the registry.</summary>
    internal static readonly RegistryProblem Internal = new(
        "internal",
        "An internal error occurred.",
        StatusCodes.Status500InternalServerError);
}

/// <summary>A problem type of the registry.</summary>
/// <param name="Name">The last segment of the type URI.</param>
/// <param name="Title">A short English summary that is the same for every occurrence.</param>
/// <param name="Status">The HTTP status.</param>
internal sealed record RegistryProblem(string Name, string Title, int Status)
{
    /// <summary>The absolute URI that identifies the type.</summary>
    internal string Type => ProblemConventions.TypePrefix + Name;
}
