// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Registration;

namespace Homeport.Platform.Registry.Domain.Tests.Unit.Registration;

public sealed class RegistrationKeysTests
{
    public sealed class Matches
    {
        [Fact]
        public void Accepts_The_Configured_Key()
        {
            Assert.True(RegistrationKeys.Matches("current", ["current"]));
        }

        [Fact]
        public void Accepts_Either_Key_During_A_Rotation()
        {
            Assert.True(RegistrationKeys.Matches("next", ["current", "next"]));
        }

        [Theory]
        [InlineData("wrong")]
        [InlineData("curren")]
        [InlineData("")]
        public void Rejects_Any_Other_Key(string presented)
        {
            Assert.False(RegistrationKeys.Matches(presented, ["current"]));
        }

        [Fact]
        public void Rejects_Every_Key_When_None_Is_Configured()
        {
            Assert.False(RegistrationKeys.Matches("current", []));
        }
    }
}
