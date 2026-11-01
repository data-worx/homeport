// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Aspire.AppHost.Tests.Integration;

/// <summary>
/// Routing from the registry end to end, when a module or the registry stops.
/// Each case stops a resource, so each gets a topology of its own.
/// </summary>
public sealed class RoutingTests
{
    // The registry drops an instance 30 s after its last heartbeat; the gateway
    // notices at its next poll, at most 5 s later.
    private static readonly TimeSpan InstanceLifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private static async Task StopAsync(RunningAppHost appHost, string resource)
    {
        var result = await appHost.App.Services.GetRequiredService<ResourceCommandService>()
            .ExecuteCommandAsync(resource, KnownResourceCommands.StopCommand);

        Assert.True(result.Success, result.Message);
    }

    public sealed class WhenEchoStops(RunningAppHost appHost) : IClassFixture<RunningAppHost>
    {
        [Fact]
        public async Task Returns_Not_Found_Within_The_Instance_Lifetime_And_One_Poll()
        {
            var gateway = await appHost.GatewayRoutingEchoAsync();

            await StopAsync(appHost, "echo");
            var status = await RunningAppHost.StatusOnceAsync(gateway, status => status == HttpStatusCode.NotFound, InstanceLifetime + PollInterval);

            Assert.Equal(HttpStatusCode.NotFound, status);
        }
    }

    public sealed class WhenTheRegistryStops(RunningAppHost appHost) : IClassFixture<RunningAppHost>
    {
        [Fact]
        public async Task Keeps_Routing_To_Echo()
        {
            var gateway = await appHost.GatewayRoutingEchoAsync();

            await StopAsync(appHost, "registry");
            await Task.Delay(PollInterval * 2);

            Assert.Equal(HttpStatusCode.OK, (await gateway.GetAsync(RunningAppHost.EchoRequestPath)).StatusCode);
        }
    }
}
