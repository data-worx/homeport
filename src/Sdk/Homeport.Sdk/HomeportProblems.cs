// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Http;

namespace Homeport.Sdk;

/// <summary>
/// Homeport's own problem types that the SDK answers with. Each one has a
/// reference page under <c>docs/reference/problems/</c>.
/// </summary>
public static class HomeportProblems
{
    /// <summary>The prefix of every Homeport problem type.</summary>
    public const string TypePrefix = "https://homeport.data-worx.de/problems/";

    /// <summary>The extension that carries the W3C trace id of the request.</summary>
    public const string TraceIdExtension = "traceId";

    /// <summary>The extension of a validation problem that lists the errors by field path.</summary>
    public const string ErrorsExtension = "errors";

    /// <summary>An unexpected failure inside the module; the body says nothing more.</summary>
    public static readonly ProblemType Internal = new(
        new Uri(TypePrefix + "internal"),
        "An internal error occurred.",
        StatusCodes.Status500InternalServerError);

    /// <summary>The request failed validation; <see cref="ErrorsExtension"/> says where.</summary>
    public static readonly ProblemType Validation = new(
        new Uri(TypePrefix + "validation"),
        "One or more validation errors occurred.",
        StatusCodes.Status400BadRequest);
}
