// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Aspire.AppHost.Tests.Integration;

public sealed class ProgramTests
{
    public sealed class Main(RunningAppHost appHost) : IClassFixture<RunningAppHost>
    {
        [Theory]
        [InlineData("postgres")]
        [InlineData("rabbitmq")]
        [InlineData("valkey")]
        [InlineData("keycloak")]
        public async Task Starts_The_Resource_Healthy(string resource)
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);

            var state = await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync(resource, timeout.Token);

            Assert.Equal(resource, state.Resource.Name);
        }
    }
}
