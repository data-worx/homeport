// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Homeport.Shared.Client.Problems;

/// <summary>
/// An error response of Homeport as a caller reads it: Problem Details
/// (RFC 9457) with the extensions of <see cref="ProblemConventions"/>.
/// </summary>
/// <param name="Type">The problem type, an absolute URI.</param>
/// <param name="Title">A short summary of the problem type, in English.</param>
/// <param name="Status">The HTTP status code.</param>
/// <param name="Detail">What went wrong in this occurrence, when the producer says.</param>
/// <param name="Instance">The occurrence, when the producer names one.</param>
/// <param name="TraceId">The W3C trace id of the request.</param>
/// <param name="Errors">The errors of a validation problem by field path; absent otherwise.</param>
public sealed record ProblemResponse(
    Uri Type,
    string Title,
    int Status,
    string? Detail,
    string? Instance,
    string TraceId,
    IReadOnlyDictionary<string, string[]>? Errors)
{
    /// <summary>The extensions a module adds beyond those of Homeport.</summary>
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? Extensions { get; init; }
}
