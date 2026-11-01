// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Sdk.Registration;

namespace Homeport.Sdk.Tests.Unit.Registration;

public sealed class RegistrationOptionsValidatorTests
{
    private static readonly RegistrationOptions Complete = new()
    {
        Url = new Uri("http://registry"),
        Key = "key",
        InstanceAddress = new Uri("http://things-1:8080"),
    };

    public sealed class Validate
    {
        private readonly RegistrationOptionsValidator validator = new();

        [Fact]
        public void Accepts_A_Module_That_Does_Not_Register()
        {
            var result = validator.Validate(null, new RegistrationOptions());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Accepts_Complete_Options()
        {
            var result = validator.Validate(null, Complete);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Rejects_A_Registry_Without_A_Key()
        {
            var result = validator.Validate(null, new RegistrationOptions { Url = Complete.Url, InstanceAddress = Complete.InstanceAddress });

            Assert.Equal(["Homeport:Registry:Key is not set. The registry accepts a module only with its registration key."], result.Failures);
        }

        [Fact]
        public void Rejects_A_Registry_Without_An_Absolute_Instance_Address()
        {
            var result = validator.Validate(null, new RegistrationOptions { Url = Complete.Url, Key = Complete.Key, InstanceAddress = new Uri("/things", UriKind.Relative) });

            Assert.Equal(["Homeport:Registry:InstanceAddress is not an absolute URI. The gateway reaches the instance there."], result.Failures);
        }
    }
}
