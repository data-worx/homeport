// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Infrastructure.Usage;

/// <summary>How the gateway retries a usage event the broker did not take.</summary>
public sealed class UsageOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "Usage";

    /// <summary>The delays when none are configured: an event rides out a broker outage of 36 seconds.</summary>
    public static readonly IReadOnlyList<TimeSpan> DefaultRetryDelays = [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)];

    /// <summary>
    /// How long each retry of a failed send waits, in memory; after the last
    /// one the event is dropped and counted. Empty means
    /// <see cref="DefaultRetryDelays"/>: the binder appends to a default an
    /// array already holds, so the default is not set here.
    /// </summary>
    public TimeSpan[] RetryDelays { get; set; } = [];

    /// <summary>The delays that apply: <see cref="RetryDelays"/>, or <see cref="DefaultRetryDelays"/>.</summary>
    public IReadOnlyList<TimeSpan> EffectiveRetryDelays => RetryDelays.Length > 0 ? RetryDelays : DefaultRetryDelays;
}
