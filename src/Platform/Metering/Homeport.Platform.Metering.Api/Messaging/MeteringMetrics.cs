// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.Metrics;

namespace Homeport.Platform.Metering.Api.Messaging;

/// <summary>Metering's own metrics, on a meter named after the service.</summary>
public sealed class MeteringMetrics
{
    /// <summary>The meter's name; the service defaults export it.</summary>
    public static readonly string MeterName = typeof(MeteringMetrics).Assembly.GetName().Name!;

    /// <summary>Counts consumed usage events, tagged with <see cref="OutcomeTag"/>.</summary>
    public const string EventsInstrument = "homeport.metering.events";

    /// <summary>Whether an event was <see cref="Recorded"/> or a <see cref="Duplicate"/>.</summary>
    public const string OutcomeTag = "homeport.metering.outcome";

    /// <summary>The event was new and counted.</summary>
    public const string Recorded = "recorded";

    /// <summary>The event had been counted before and was discarded.</summary>
    public const string Duplicate = "duplicate";

    private readonly Counter<long> events;

    public MeteringMetrics(IMeterFactory meters)
    {
        events = meters.Create(MeterName).CreateCounter<long>(EventsInstrument, description: "Usage events metering consumed.");
    }

    /// <summary>Counts one consumed event.</summary>
    /// <param name="recorded">Whether it was new, rather than a duplicate.</param>
    public void Consumed(bool recorded)
    {
        events.Add(1, new KeyValuePair<string, object?>(OutcomeTag, recorded ? Recorded : Duplicate));
    }
}
