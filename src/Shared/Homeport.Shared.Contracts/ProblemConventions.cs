// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Contracts;

/// <summary>
/// The parts of Homeport's error format (ADR-0016) that the gateway and the
/// SDK both write: Problem Details (RFC 9457) with these extensions.
/// </summary>
public static class ProblemConventions
{
    /// <summary>The prefix of every Homeport problem type.</summary>
    public const string TypePrefix = "https://homeport.data-worx.de/problems/";

    /// <summary>The extension that carries the W3C trace id of the request.</summary>
    public const string TraceIdExtension = "traceId";

    /// <summary>The extension of a validation problem that lists the errors by field path.</summary>
    public const string ErrorsExtension = "errors";
}
