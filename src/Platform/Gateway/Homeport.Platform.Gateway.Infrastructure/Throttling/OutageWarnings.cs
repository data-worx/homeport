// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Infrastructure.Throttling;

/// <summary>
/// Lets an outage be logged at most once per <see cref="Interval"/>, however
/// many requests run into it at once.
/// </summary>
internal sealed class OutageWarnings(TimeProvider time)
{
    /// <summary>How often an outage is logged at most.</summary>
    internal static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    private const long Never = long.MinValue;

    private long lastWarning = Never;

    /// <summary>Whether the caller is the one to log the outage now.</summary>
    public bool TryWarn()
    {
        var now = time.GetTimestamp();
        var last = Interlocked.Read(ref lastWarning);
        if (last != Never && time.GetElapsedTime(last, now) < Interval)
        {
            return false;
        }

        // Of the requests that see the interval passed, only one wins.
        return Interlocked.CompareExchange(ref lastWarning, now, last) == last;
    }
}
