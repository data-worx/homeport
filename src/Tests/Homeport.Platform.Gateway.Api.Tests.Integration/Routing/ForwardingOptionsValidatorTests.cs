// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Routing;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Routing;

public sealed class ForwardingOptionsValidatorTests
{
    public sealed class Validate
    {
        [Fact]
        public void Accepts_The_Default()
        {
            var result = new ForwardingOptionsValidator().Validate(null, new ForwardingOptions());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Refuses_A_Timeout_Of_Zero()
        {
            var result = new ForwardingOptionsValidator().Validate(null, new ForwardingOptions { Timeout = TimeSpan.Zero });

            Assert.True(result.Failed);
        }

        [Fact]
        public void Refuses_A_Negative_Timeout()
        {
            var result = new ForwardingOptionsValidator().Validate(null, new ForwardingOptions { Timeout = TimeSpan.FromSeconds(-1) });

            Assert.True(result.Failed);
        }
    }
}
