// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>
/// Records the label names of every measurement on an instrument named
/// <c>homeport.*</c>, in every meter of the process, while it lives.
/// </summary>
internal sealed class MetricLabels : IDisposable
{
    private const string HomeportPrefix = "homeport.";

    private readonly MeterListener listener = new();
    private readonly ConcurrentDictionary<(string Instrument, string Label), byte> seen = new();

    public MetricLabels()
    {
        listener.InstrumentPublished = (instrument, subscription) =>
        {
            if (instrument.Name.StartsWith(HomeportPrefix, StringComparison.Ordinal))
            {
                subscription.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, _, tags, _) => Record(instrument, tags));
        listener.SetMeasurementEventCallback<int>((instrument, _, tags, _) => Record(instrument, tags));
        listener.SetMeasurementEventCallback<double>((instrument, _, tags, _) => Record(instrument, tags));
        listener.Start();
    }

    /// <summary>Every instrument that was measured with a label, and the label.</summary>
    internal IReadOnlyCollection<(string Instrument, string Label)> Seen => seen.Keys.ToList();

    public void Dispose() => listener.Dispose();

    private void Record(Instrument instrument, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        foreach (var tag in tags)
        {
            seen.TryAdd((instrument.Name, tag.Key), 0);
        }
    }
}
