// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

public sealed class ProgramTests
{
    public sealed class Main(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";

        [Fact]
        public async Task Forwards_The_Path_Below_The_Module_Prefix()
        {
            var forwarded = await gateway.CreateClient().GetFromJsonAsync<ForwardedRequest>("/modules/echo/v1/request");

            Assert.Equal("/v1/request", forwarded?.Path);
        }

        [Fact]
        public async Task Strips_Incoming_Homeport_Headers()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/modules/echo/v1/request");
            request.Headers.Add("X-Homeport-Tenant-Id", "6f1c2a8e-0000-4000-8000-000000000001");
            request.Headers.Add("x-homeport-client-id", "spoofed");

            var forwarded = await Send(request);

            Assert.DoesNotContain(forwarded.Headers.Keys, name => name.StartsWith("x-homeport-", StringComparison.Ordinal));
        }

        [Fact]
        public async Task Keeps_Other_Headers()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/modules/echo/v1/request");
            request.Headers.Add("X-Probe", "anchor");

            var forwarded = await Send(request);

            Assert.Equal("anchor", forwarded.Headers["x-probe"]);
        }

        [Fact]
        public async Task Propagates_The_Trace_Id()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/modules/echo/v1/request");
            request.Headers.Add("traceparent", $"00-{TraceId}-00f067aa0ba902b7-01");

            var forwarded = await Send(request);

            Assert.Equal(TraceId, forwarded.Headers["traceparent"].Split('-')[1]);
        }

        [Fact]
        public async Task Answers_Not_Found_For_An_Unknown_Module()
        {
            var response = await gateway.CreateClient().GetAsync("/modules/unknown/v1/request");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Does_Not_Start_Without_A_Signing_Key()
        {
            await using var unconfigured = new WebApplicationFactory<Program>();

            Assert.Throws<OptionsValidationException>(() => unconfigured.CreateClient());
        }

        private async Task<ForwardedRequest> Send(HttpRequestMessage request)
        {
            var response = await gateway.CreateClient().SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ForwardedRequest>()
                ?? throw new InvalidOperationException("The stand-in module answered without a body.");
        }
    }
}
