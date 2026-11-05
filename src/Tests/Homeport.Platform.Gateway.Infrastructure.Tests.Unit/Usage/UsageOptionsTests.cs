// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.Usage;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.Usage;

public sealed class UsageOptionsTests
{
    public sealed class EffectiveRetryDelays
    {
        [Fact]
        public void Returns_The_Defaults_When_None_Are_Configured()
        {
            Assert.Equal(UsageOptions.DefaultRetryDelays, new UsageOptions().EffectiveRetryDelays);
        }

        [Fact]
        public void Returns_The_Configured_Delays_Instead_Of_The_Defaults()
        {
            var options = new UsageOptions { RetryDelays = [TimeSpan.FromSeconds(2)] };

            Assert.Equal([TimeSpan.FromSeconds(2)], options.EffectiveRetryDelays);
        }
    }
}
