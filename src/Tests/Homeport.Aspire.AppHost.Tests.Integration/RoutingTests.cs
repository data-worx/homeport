// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
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

    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ReachableTimeout = TimeSpan.FromSeconds(3);

    // Stops the resource and waits until it no longer runs. The command's own
    // result is not the measure: on Linux, Aspire runs a project through
    // `dotnet run`, loses track of the process once it stops, and reports the
    // stop as failed after its state turned Unknown rather than Finished.
    private static async Task StopAsync(RunningAppHost appHost, string resource)
    {
        await appHost.App.Services.GetRequiredService<ResourceCommandService>()
            .ExecuteCommandAsync(resource, KnownResourceCommands.StopCommand);

        using var timeout = new CancellationTokenSource(StopTimeout);
        await appHost.App.ResourceNotifications.WaitForResourceAsync(
            resource,
            change => change.Snapshot.State?.Text != KnownResourceStates.Running && change.Snapshot.State?.Text != KnownResourceStates.Stopping,
            timeout.Token);
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
            Assert.True(await RegistryAnswersAsync(), "The registry does not answer before it was stopped.");

            await StopAsync(appHost, "registry");
            await Task.Delay(PollInterval * 2);

            Assert.False(await RegistryAnswersAsync(), "The registry still answers after it was stopped.");
            Assert.Equal(HttpStatusCode.OK, (await gateway.GetAsync(RunningAppHost.EchoRequestPath)).StatusCode);
        }

        // Unreachable or hanging both count as stopped: locally, Aspire's proxy
        // in front of a stopped project may accept a connection and never answer.
        private async Task<bool> RegistryAnswersAsync()
        {
            using var timeout = new CancellationTokenSource(ReachableTimeout);
            try
            {
                var response = await appHost.App.CreateHttpClient("registry").GetAsync("/health/live", timeout.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException)
            {
                return false;
            }
        }
    }
}
