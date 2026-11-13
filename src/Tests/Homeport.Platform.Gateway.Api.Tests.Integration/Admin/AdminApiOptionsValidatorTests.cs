// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Admin;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Admin;

public sealed class AdminApiOptionsValidatorTests
{
    public sealed class Validate
    {
        [Fact]
        public void Accepts_No_Port_Without_An_Identity_Provider()
        {
            var result = new AdminApiOptionsValidator().Validate(null, new AdminApiOptions());

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Accepts_A_Port_With_Authority_And_Audience()
        {
            var result = new AdminApiOptionsValidator().Validate(null, new AdminApiOptions
            {
                Port = 8081,
                Authority = new Uri("https://idp.example/realms/homeport"),
                Audience = "homeport-portal",
            });

            Assert.True(result.Succeeded);
        }

        [Fact]
        public void Refuses_A_Port_Without_Authority_And_Audience()
        {
            var result = new AdminApiOptionsValidator().Validate(null, new AdminApiOptions { Port = 8081 });

            Assert.Equal(2, result.Failures?.Count());
        }

        [Fact]
        public void Refuses_A_Port_Out_Of_Range()
        {
            var result = new AdminApiOptionsValidator().Validate(null, new AdminApiOptions
            {
                Port = 0,
                Authority = new Uri("https://idp.example/realms/homeport"),
                Audience = "homeport-portal",
            });

            Assert.True(result.Failed);
        }

        [Fact]
        public void Refuses_An_Empty_Claim()
        {
            var result = new AdminApiOptionsValidator().Validate(null, new AdminApiOptions
            {
                Port = 8081,
                Authority = new Uri("https://idp.example/realms/homeport"),
                Audience = "homeport-portal",
                ClaimValue = string.Empty,
            });

            Assert.True(result.Failed);
        }
    }
}
