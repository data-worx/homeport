// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Sdk;

/// <summary>
/// A kind of error a module answers with, as Problem Details (RFC 9457) name
/// it. A module declares its types in one place and answers with them through
/// <see cref="ToResult"/>.
/// </summary>
public sealed record ProblemType
{
    private const int LowestErrorStatus = StatusCodes.Status400BadRequest;
    private const int HighestErrorStatus = 599;

    /// <summary>Declares a problem type.</summary>
    /// <param name="type">The absolute URI that identifies the type; a module uses a prefix of its own.</param>
    /// <param name="title">A short English summary that is the same for every occurrence.</param>
    /// <param name="status">The HTTP status, 4xx or 5xx.</param>
    /// <exception cref="ArgumentException">The URI is relative, or the title is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The status is not an error status.</exception>
    public ProblemType(Uri type, string title, int status)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentOutOfRangeException.ThrowIfLessThan(status, LowestErrorStatus);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(status, HighestErrorStatus);

        if (!type.IsAbsoluteUri)
        {
            throw new ArgumentException($"A problem type is an absolute URI; '{type}' is relative.", nameof(type));
        }

        Type = type;
        Title = title;
        Status = status;
    }

    /// <summary>The absolute URI that identifies the type.</summary>
    public Uri Type { get; }

    /// <summary>A short English summary that is the same for every occurrence.</summary>
    public string Title { get; }

    /// <summary>The HTTP status.</summary>
    public int Status { get; }

    /// <summary>
    /// An action result that answers with this type and <paramref name="detail"/>
    /// as <c>application/problem+json</c>, carrying the <c>traceId</c> extension.
    /// </summary>
    /// <param name="detail">An English explanation of this occurrence, for the caller's developer.</param>
    public ActionResult ToResult(string detail)
    {
        return new ProblemTypeResult(this, detail);
    }
}
