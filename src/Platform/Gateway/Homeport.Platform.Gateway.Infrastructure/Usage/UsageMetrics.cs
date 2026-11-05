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

    /// <summary>Why an event was dropped: <see cref="NotPublished"/> or <see cref="RetriesExhausted"/>.</summary>
    public const string ReasonTag = "homeport.gateway.usage.reason";

    /// <summary>Publishing failed before the event was handed to the sender.</summary>
    public const string NotPublished = "not_published";

    /// <summary>The broker did not take the event within every retry.</summary>
    public const string RetriesExhausted = "retries_exhausted";

    private readonly Counter<long> dropped;

    public UsageMetrics(IMeterFactory meters)
    {
        dropped = meters.Create(MeterName).CreateCounter<long>(DroppedInstrument, description: "Usage events the gateway could not deliver.");
    }

    /// <summary>Counts one dropped event for <paramref name="reason"/>.</summary>
    public void Dropped(string reason)
    {
        dropped.Add(1, new KeyValuePair<string, object?>(ReasonTag, reason));
    }
}
