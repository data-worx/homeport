// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.Throttling;
using Microsoft.Extensions.Time.Testing;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.Throttling;

public sealed class OutageWarningsTests
{
    public sealed class TryWarn
    {
        private readonly FakeTimeProvider time = new();

        [Fact]
        public void Warns_About_The_First_Failure()
        {
            var warnings = new OutageWarnings(time);

            Assert.True(warnings.TryWarn());
        }

        [Fact]
        public void Does_Not_Warn_Again_Within_The_Interval()
        {
            var warnings = new OutageWarnings(time);
            warnings.TryWarn();
            time.Advance(OutageWarnings.Interval - TimeSpan.FromSeconds(1));

            Assert.False(warnings.TryWarn());
        }

        [Fact]
        public void Warns_Again_Once_The_Interval_Passed()
        {
            var warnings = new OutageWarnings(time);
            warnings.TryWarn();
            time.Advance(OutageWarnings.Interval);

            Assert.True(warnings.TryWarn());
        }
    }
}
