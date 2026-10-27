// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

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
        [InlineData("echo")]
        public async Task Starts_The_Resource_Healthy(string resource)
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);

            var state = await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync(resource, timeout.Token);

            Assert.Equal(resource, state.Resource.Name);
        }

        [Fact]
        public async Task Serves_The_Echo_Manifest()
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);
            await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync("echo", timeout.Token);

            var response = await appHost.App.CreateHttpClient("echo")
                .GetStringAsync("/.well-known/homeport/manifest", timeout.Token);

            Assert.Equal("echo", JsonDocument.Parse(response).RootElement.GetProperty("id").GetString());
        }
    }
}
