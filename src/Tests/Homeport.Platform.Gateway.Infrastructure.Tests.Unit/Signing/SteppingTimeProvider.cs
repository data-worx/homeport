// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.Signing;

/// <summary>A clock that stands still until a test moves it.</summary>
internal sealed class SteppingTimeProvider : TimeProvider
{
    private DateTimeOffset now = TimeProvider.System.GetUtcNow();

    public override DateTimeOffset GetUtcNow() => now;

    internal void Advance(TimeSpan by)
    {
        now += by;
    }
}
