// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

public sealed class ProgramTests
{
    public sealed class Main(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private const string TraceId = "4bf92f3577b34da6a3ce929d0e0e4736";

        // A port nothing listens on, so the gateway cannot reach the module.
        private const string ClosedEndpoint = "http://127.0.0.1:1";

        private const string ProblemTypePrefix = "https://homeport.data-worx.de/problems/";

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
        public async Task Answers_An_Unknown_Module_As_Unknown_Module_Problem()
        {
            using var response = await gateway.CreateClient().SendAsync(Traced("/modules/unknown/v1/request"));

            var problem = await ReadProblem(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(ProblemTypePrefix + "unknown-module", problem["type"]?.GetValue<string>());
            Assert.Equal(TraceId, problem["traceId"]?.GetValue<string>());
        }

        [Fact]
        public async Task Passes_A_Module_Problem_Through_With_The_Trace_Id_Added()
        {
            using var response = await gateway.CreateClient().SendAsync(Traced(ModulePath(ForwardingGateway.NotFoundProblemPath)));

            var problem = await ReadProblem(response);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(ForwardingGateway.NotFoundType, problem["type"]?.GetValue<string>());
            Assert.Equal(ForwardingGateway.NotFoundDetail, problem["detail"]?.GetValue<string>());
            Assert.Equal(TraceId, problem["traceId"]?.GetValue<string>());
        }

        [Fact]
        public async Task Replaces_The_Body_Of_A_Module_Server_Error()
        {
            using var response = await gateway.CreateClient().SendAsync(Traced(ModulePath(ForwardingGateway.InternalProblemPath)));

            var problem = await ReadProblem(response);
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(ProblemTypePrefix + "internal", problem["type"]?.GetValue<string>());
            Assert.Null(problem["detail"]);
            Assert.Equal(TraceId, problem["traceId"]?.GetValue<string>());
        }

        [Fact]
        public async Task Replaces_A_Module_Error_Body_That_Is_Not_Problem_Details()
        {
            using var response = await gateway.CreateClient().SendAsync(Traced(ModulePath(ForwardingGateway.HtmlBadRequestPath)));

            var problem = await ReadProblem(response);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal(StatusCodes.Status400BadRequest, problem["status"]?.GetValue<int>());
            Assert.Equal(TraceId, problem["traceId"]?.GetValue<string>());
        }

        [Fact]
        public async Task Answers_An_Unreachable_Module_As_Module_Unreachable_Problem()
        {
            await using var unreachable = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host => host.WithSigningKey().UseSetting(ForwardingGateway.EchoEndpointKey, ClosedEndpoint));

            using var response = await unreachable.CreateClient().SendAsync(Traced("/modules/echo/v1/request"));

            var problem = await ReadProblem(response);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
            Assert.Equal(ProblemTypePrefix + "module-unreachable", problem["type"]?.GetValue<string>());
            Assert.Equal(TraceId, problem["traceId"]?.GetValue<string>());
        }

        [Fact]
        public async Task Does_Not_Start_Without_A_Signing_Key()
        {
            await using var unconfigured = new WebApplicationFactory<Program>();

            Assert.Throws<OptionsValidationException>(() => unconfigured.CreateClient());
        }

        private static string ModulePath(string path) => "/modules/echo" + path;

        private static HttpRequestMessage Traced(string path)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("traceparent", $"00-{TraceId}-00f067aa0ba902b7-01");

            return request;
        }

        // Asserts the media type, since a body that parses is not yet Problem Details.
        private static async Task<JsonObject> ReadProblem(HttpResponseMessage response)
        {
            Assert.Equal(MediaTypeNames.Application.ProblemJson, response.Content.Headers.ContentType?.MediaType);

            return await response.Content.ReadFromJsonAsync<JsonObject>()
                ?? throw new InvalidOperationException("The gateway answered without a body.");
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
