// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

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
        [InlineData("gateway")]
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

        [Fact]
        public void Exposes_Only_The_Gateway_Externally()
        {
            var model = appHost.App.Services.GetRequiredService<DistributedApplicationModel>();

            var external = model.Resources
                .Where(resource => resource.Annotations.OfType<EndpointAnnotation>().Any(endpoint => endpoint.IsExternal))
                .Select(resource => resource.Name);

            Assert.Equal("gateway", Assert.Single(external));
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
        public async Task Strips_Homeport_Headers_Before_Echo()
        {
            var gateway = await Gateway();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/modules/echo/v1/request");
            request.Headers.Add("X-Homeport-Tenant-Id", "6f1c2a8e-0000-4000-8000-000000000001");

            var headers = await ReceivedHeaders(gateway, request);

            Assert.False(headers.TryGetProperty("x-homeport-tenant-id", out _));
        }

        [Fact]
        public async Task Propagates_The_Trace_Id_To_Echo()
        {
            const string traceId = "4bf92f3577b34da6a3ce929d0e0e4736";
            var gateway = await Gateway();
            using var request = new HttpRequestMessage(HttpMethod.Get, "/modules/echo/v1/request");
            request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");

            var headers = await ReceivedHeaders(gateway, request);

            Assert.Equal(traceId, headers.GetProperty("traceparent").GetString()?.Split('-')[1]);
        }

        [Fact]
        public async Task Publishes_The_Signing_Key_Through_The_Gateway()
        {
            var gateway = await Gateway();

            var jwks = JsonDocument.Parse(await gateway.GetStringAsync("/.well-known/jwks.json")).RootElement;

            Assert.Equal("local", Assert.Single(jwks.GetProperty("keys").EnumerateArray()).GetProperty("kid").GetString());
        }

        private async Task<HttpClient> Gateway()
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);
            await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync("echo", timeout.Token);
            await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync("gateway", timeout.Token);

            return appHost.App.CreateHttpClient("gateway");
        }

        private static async Task<JsonElement> ReceivedHeaders(HttpClient gateway, HttpRequestMessage request)
        {
            var response = await gateway.SendAsync(request);
            response.EnsureSuccessStatusCode();

            return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("headers");
        }
    }
}
