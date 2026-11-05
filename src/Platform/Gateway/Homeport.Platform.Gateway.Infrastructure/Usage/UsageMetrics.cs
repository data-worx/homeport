// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.Metrics;

namespace Homeport.Platform.Gateway.Infrastructure.Usage;

/// <summary>The gateway's usage metrics, on the meter of the gateway's host.</summary>
public sealed class UsageMetrics
{
    /// <summary>The meter's name: the gateway host's application name, which the service defaults export.</summary>
    public const string MeterName = "Homeport.Platform.Gateway.Api";

    /// <summary>Counts usage events that never reached the broker, tagged with <see cref="ReasonTag"/>.</summary>
    public const string DroppedInstrument = "homeport.gateway.usage.dropped";

    /// <summary>Counts quantities a module reported that were rejected for the route's default, tagged with <see cref="ReasonTag"/>.</summary>
    public const string RejectedInstrument = "homeport.gateway.usage.rejected";

    /// <summary>
    /// Why an event was dropped, <see cref="NotPublished"/> or <see cref="RetriesExhausted"/>,
    /// or why a reported quantity was rejected: the code of the rule it broke.
    /// </summary>
    public const string ReasonTag = "homeport.gateway.usage.reason";

    /// <summary>Publishing failed before the event was handed to the sender.</summary>
    public const string NotPublished = "not_published";

    /// <summary>The broker did not take the event within every retry.</summary>
    public const string RetriesExhausted = "retries_exhausted";

    private readonly Counter<long> dropped;
    private readonly Counter<long> rejected;

    public UsageMetrics(IMeterFactory meters)
    {
        var meter = meters.Create(MeterName);
        dropped = meter.CreateCounter<long>(DroppedInstrument, description: "Usage events the gateway could not deliver.");
        rejected = meter.CreateCounter<long>(RejectedInstrument, description: "Quantities reported by a module that the gateway rejected.");
    }

    /// <summary>Counts one dropped event for <paramref name="reason"/>.</summary>
    public void Dropped(string reason)
    {
        dropped.Add(1, new KeyValuePair<string, object?>(ReasonTag, reason));
    }

    /// <summary>Counts one rejected report for <paramref name="reason"/>.</summary>
    public void Rejected(string reason)
    {
        rejected.Add(1, new KeyValuePair<string, object?>(ReasonTag, reason));
    }
}
