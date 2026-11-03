// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Aspire.AppHost.Tests.Integration;

public sealed class ProgramTests
{
    [Collection(AppHostCollection.Name)]
    public sealed class Main(RunningAppHost appHost)
    {
        [Theory]
        [InlineData("postgres")]
        [InlineData("rabbitmq")]
        [InlineData("valkey")]
        [InlineData("keycloak")]
        [InlineData("registry")]
        [InlineData("echo")]
        [InlineData("gateway")]
        [InlineData("control-plane")]
        public async Task Starts_The_Resource_Healthy(string resource)
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);

            var state = await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync(resource, timeout.Token);

            Assert.Equal(resource, state.Resource.Name);
        }

        [Fact]
        public void Starts_The_Control_Plane_Only_After_Migrate_Completed()
        {
            var model = appHost.App.Services.GetRequiredService<DistributedApplicationModel>();

            var waits = model.Resources.Single(resource => resource.Name == "control-plane")
                .Annotations.OfType<WaitAnnotation>()
                .Select(wait => (wait.Resource.Name, wait.WaitType));

            Assert.Contains(("control-plane-migrate", WaitType.WaitForCompletion), waits);
        }

        [Fact]
        public async Task Has_Completed_Migrate_Successfully_Once_The_Control_Plane_Is_Healthy()
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);
            await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync("control-plane", timeout.Token);

            appHost.App.ResourceNotifications.TryGetCurrentState("control-plane-migrate", out var migrate);

            Assert.Equal((KnownResourceStates.Finished, 0), (migrate?.Snapshot.State?.Text, migrate?.Snapshot.ExitCode));
        }

        [Fact]
        public void Exposes_Only_The_Gateway_And_Keycloak_Externally()
        {
            var model = appHost.App.Services.GetRequiredService<DistributedApplicationModel>();

            var external = model.Resources
                .Where(resource => resource.Annotations.OfType<EndpointAnnotation>().Any(endpoint => endpoint.IsExternal))
                .Select(resource => resource.Name)
                .Order(StringComparer.Ordinal);

            Assert.Equal(["gateway", "keycloak"], external);
        }

        [Fact]
        public async Task Returns_The_Body_Echo_Received_Through_The_Gateway()
        {
            var gateway = await Gateway();
            using var content = new StringContent("""{"hello":"Homeport ⚓"}""", Encoding.UTF8, "application/json");

            var response = await gateway.PostAsync("/modules/echo/v1/echo", content);

            Assert.Equal("""{"hello":"Homeport ⚓"}""", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Answers_A_Client_Unknown_To_The_Control_Plane_With_Forbidden()
        {
            await Gateway();
            var unmapped = appHost.App.CreateHttpClient("gateway");
            unmapped.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await appHost.TokenAsync(RunningAppHost.UnmappedClient));
            using var content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await unmapped.PostAsync("/modules/echo/v1/echo", content);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private Task<HttpClient> Gateway() => appHost.GatewayRoutingEchoAsync();
    }
}
