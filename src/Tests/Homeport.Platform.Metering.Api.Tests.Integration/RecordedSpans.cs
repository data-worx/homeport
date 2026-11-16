// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Diagnostics;
using OpenTelemetry;

namespace Homeport.Platform.Metering.Api.Tests.Integration;

/// <summary>
/// The spans metering's tracer provider ends, in the order it ends them:
/// what metering exports, not what any listener in the process could see.
/// </summary>
public sealed class RecordedSpans : BaseProcessor<Activity>
{
    private readonly ConcurrentQueue<Activity> spans = new();

    /// <summary>The spans of the trace <paramref name="traceId"/>.</summary>
    internal IReadOnlyList<Activity> Of(ActivityTraceId traceId) => spans.Where(span => span.TraceId == traceId).ToList();

    public override void OnEnd(Activity data) => spans.Enqueue(data);
}
