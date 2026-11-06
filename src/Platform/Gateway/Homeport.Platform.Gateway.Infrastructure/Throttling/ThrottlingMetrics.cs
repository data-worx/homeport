// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.Metrics;
using Homeport.Platform.Gateway.Infrastructure.Usage;

namespace Homeport.Platform.Gateway.Infrastructure.Throttling;

/// <summary>The gateway's throttling metrics, on the meter of the gateway's host.</summary>
public sealed class ThrottlingMetrics
{
    /// <summary>Counts requests rejected with <c>429</c> for their tenant's rate limit.</summary>
    public const string ThrottledInstrument = "homeport.gateway.throttled";

    /// <summary>Counts requests that passed unthrottled because the token buckets could not be reached.</summary>
    public const string UnavailableInstrument = "homeport.gateway.ratelimit.unavailable";

    private readonly Counter<long> throttled;
    private readonly Counter<long> unavailable;

    public ThrottlingMetrics(IMeterFactory meters)
    {
        var meter = meters.Create(UsageMetrics.MeterName);
        throttled = meter.CreateCounter<long>(ThrottledInstrument, description: "Requests the gateway rejected for their tenant's rate limit.");
        unavailable = meter.CreateCounter<long>(UnavailableInstrument, description: "Requests that passed unthrottled while the token buckets could not be reached.");
    }

    /// <summary>Counts one rejected request.</summary>
    public void Throttled()
    {
        throttled.Add(1);
    }

    /// <summary>Counts one request that passed unthrottled.</summary>
    public void Unavailable()
    {
        unavailable.Add(1);
    }
}
