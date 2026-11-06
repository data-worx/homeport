// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Domain.Throttling;

/// <summary>
/// The rate limit of a plan: a token bucket per tenant that holds
/// <see cref="Burst"/> requests and refills at <see cref="RequestsPerSecond"/>.
/// </summary>
public sealed record RateLimit
{
    /// <summary>A limit of <paramref name="requestsPerSecond"/> with room for <paramref name="burst"/> requests at once.</summary>
    /// <param name="requestsPerSecond">How fast the bucket refills; at least one.</param>
    /// <param name="burst">How many requests the bucket holds; at least one.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either value is less than one.</exception>
    public RateLimit(int requestsPerSecond, int burst)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(requestsPerSecond, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(burst, 1);

        RequestsPerSecond = requestsPerSecond;
        Burst = burst;
    }

    /// <summary>How many requests per second the bucket refills.</summary>
    public int RequestsPerSecond { get; }

    /// <summary>How many requests the bucket holds.</summary>
    public int Burst { get; }

    /// <summary>How long an empty bucket takes to fill.</summary>
    public TimeSpan FillTime => TimeSpan.FromSeconds((double)Burst / RequestsPerSecond);
}
