// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.Signing;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.Signing;

public sealed class ModuleTokenOptionsValidatorTests
{
    public sealed class Validate
    {
        private readonly ModuleTokenOptionsValidator validator = new();

        [Fact]
        public void Succeeds_For_The_Default_Lifetime()
        {
            var result = validator.Validate(null, new ModuleTokenOptions());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Succeeds_For_The_Maximum_Lifetime()
        {
            var result = validator.Validate(null, new ModuleTokenOptions { Lifetime = TimeSpan.FromMinutes(5) });

            Assert.True(result.Succeeded);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Fails_For_A_Lifetime_That_Is_Not_Positive(int seconds)
        {
            var result = validator.Validate(null, new ModuleTokenOptions { Lifetime = TimeSpan.FromSeconds(seconds) });

            Assert.True(result.Failed);
        }

        [Fact]
        public void Fails_For_A_Lifetime_Longer_Than_A_Gateway_Token_May_Live()
        {
            var result = validator.Validate(null, new ModuleTokenOptions { Lifetime = TimeSpan.FromMinutes(5).Add(TimeSpan.FromSeconds(1)) });

            Assert.True(result.Failed);
        }
    }
}
