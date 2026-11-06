// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Domain.Throttling;

/// <summary>
/// What a request is told about its tenant's rate limit: whether it passes,
/// and the limit in whole seconds, as a caller reads it.
/// </summary>
/// <param name="Allowed">Whether the request passes.</param>
/// <param name="RetryAfterSeconds">For a rejected request, the whole seconds until a token is free again; at least one. Zero for one that passes.</param>
/// <param name="Quota">How many requests the limit allows in <paramref name="WindowSeconds"/>: the bucket's size.</param>
/// <param name="WindowSeconds">The whole seconds an empty bucket takes to fill; at least one.</param>
/// <param name="Remaining">The requests left before the limit is reached.</param>
/// <param name="ResetSeconds">The whole seconds until the bucket is full again.</param>
public sealed record ThrottleDecision(
    bool Allowed,
    int RetryAfterSeconds,
    int Quota,
    int WindowSeconds,
    int Remaining,
    int ResetSeconds)
{
    private const int MinimumSeconds = 1;

    /// <summary>The decision for a request that left the bucket in <paramref name="state"/>.</summary>
    /// <param name="limit">The rate limit of the tenant's plan.</param>
    /// <param name="state">The bucket after the request tried to take a token.</param>
    public static ThrottleDecision From(RateLimit limit, BucketState state)
    {
        ArgumentNullException.ThrowIfNull(limit);
        ArgumentNullException.ThrowIfNull(state);

        // Rounded up, so a caller that waits as long as it is told finds a token.
        var retryAfter = state.Taken ? 0 : Math.Max(MinimumSeconds, WholeSeconds(state.NextToken));

        return new ThrottleDecision(
            state.Taken,
            retryAfter,
            limit.Burst,
            Math.Max(MinimumSeconds, WholeSeconds(limit.FillTime)),
            Math.Clamp(state.Remaining, 0, limit.Burst),
            WholeSeconds(state.Full));
    }

    private static int WholeSeconds(TimeSpan duration)
    {
        return duration <= TimeSpan.Zero ? 0 : (int)Math.Ceiling(duration.TotalSeconds);
    }
}
