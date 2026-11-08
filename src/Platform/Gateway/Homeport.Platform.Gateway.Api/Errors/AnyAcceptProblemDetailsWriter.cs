// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;

namespace Homeport.Platform.Gateway.Api.Errors;

/// <summary>
/// Writes the error body for a caller whose <c>Accept</c> header names no
/// JSON media type, such as <c>text/csv</c>. ASP.NET's own writer refuses
/// such a caller, so the error would leave as an exception with an empty
/// body; every error response is Problem Details whatever the caller asked
/// for (ADR-0016).
/// </summary>
/// <remarks>
/// Registered after ASP.NET's writer, it is asked only once that one has
/// refused. It sets <c>Accept</c> to Problem Details and hands the error back
/// to <see cref="IProblemDetailsService"/>, so ASP.NET's writer shapes the
/// body as for every other caller, <see cref="GatewayProblemDetails"/>
/// included. The request has been forwarded or rejected by then, so nothing
/// reads its <c>Accept</c> header afterwards.
/// </remarks>
internal sealed class AnyAcceptProblemDetailsWriter : IProblemDetailsWriter
{
    public bool CanWrite(ProblemDetailsContext context)
    {
        return true;
    }

    public ValueTask WriteAsync(ProblemDetailsContext context)
    {
        context.HttpContext.Request.Headers.Accept = MediaTypeNames.Application.ProblemJson;

        // From the request rather than the constructor: the service is built
        // from every writer, this one included.
        return context.HttpContext.RequestServices.GetRequiredService<IProblemDetailsService>().WriteAsync(context);
    }
}
