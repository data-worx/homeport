// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Infrastructure.Keys;

namespace Homeport.Platform.Registry.Infrastructure.Tests.Unit.Keys;

public sealed class RegistrationKeyOptionsValidatorTests
{
    private static RegistrationKeyOptions Options(params string[] keys)
    {
        return new RegistrationKeyOptions { RegistrationKeys = { ["echo"] = keys } };
    }

    public sealed class Validate
    {
        private readonly RegistrationKeyOptionsValidator validator = new();

        [Fact]
        public void Accepts_No_Module_At_All()
        {
            var result = validator.Validate(null, new RegistrationKeyOptions());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Accepts_Two_Keys_During_A_Rotation()
        {
            var result = validator.Validate(null, Options("current", "next"));

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Rejects_A_Third_Key()
        {
            var result = validator.Validate(null, Options("a", "b", "c"));

            Assert.Equal(["Registry:RegistrationKeys:echo holds 3 keys; at most 2 are valid at once."], result.Failures);
        }

        [Fact]
        public void Rejects_The_Reserved_Module_Id_Platform()
        {
            var options = new RegistrationKeyOptions { RegistrationKeys = { ["platform"] = ["current"] } };

            var result = validator.Validate(null, options);

            Assert.Equal(["Registry:RegistrationKeys:platform names the reserved module id 'platform'."], result.Failures);
        }

        [Fact]
        public void Rejects_A_Module_Id_With_The_Reserved_Prefix()
        {
            var options = new RegistrationKeyOptions { RegistrationKeys = { ["homeport-metering"] = ["current"] } };

            var result = validator.Validate(null, options);

            Assert.Equal(["Registry:RegistrationKeys:homeport-metering names a module id with the reserved prefix 'homeport-'."], result.Failures);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        public void Rejects_An_Empty_Key(string key)
        {
            var result = validator.Validate(null, Options("current", key));

            Assert.Equal(["Registry:RegistrationKeys:echo holds an empty key."], result.Failures);
        }
    }
}
