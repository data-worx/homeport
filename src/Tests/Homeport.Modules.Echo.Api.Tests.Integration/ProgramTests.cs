// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Homeport.Modules.Echo.Api.Tests.Integration;

public sealed class ProgramTests
{
    public sealed class Main(WebApplicationFactory<Program> echo) : IClassFixture<WebApplicationFactory<Program>>
    {
        [Fact]
        public async Task Serves_A_Manifest_With_Contract_Version_0()
        {
            var response = await echo.CreateClient().GetStringAsync("/.well-known/homeport/manifest");

            var root = JsonDocument.Parse(response).RootElement;
            Assert.Equal(0, root.GetProperty("contractVersion").GetInt32());
        }

        [Fact]
        public async Task Declares_The_Echo_Route_With_Its_Scope_And_Meter()
        {
            var response = await echo.CreateClient().GetStringAsync("/.well-known/homeport/manifest");

            var root = JsonDocument.Parse(response).RootElement;
            Assert.Equal("echo", root.GetProperty("id").GetString());
            Assert.Equal(
                """{"method":"POST","path":"/v1/echo","scope":"echo.invoke","meter":"requests","quantity":1}""",
                root.GetProperty("routes")[0].GetRawText());
        }

        [Fact]
        public async Task Declares_The_Request_Route_With_Its_Scope_And_Meter()
        {
            var response = await echo.CreateClient().GetStringAsync("/.well-known/homeport/manifest");

            var root = JsonDocument.Parse(response).RootElement;
            Assert.Equal(
                """{"method":"GET","path":"/v1/request","scope":"echo.invoke","meter":"requests","quantity":1}""",
                root.GetProperty("routes")[1].GetRawText());
        }

        [Theory]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public async Task Answers_Health(string path)
        {
            var response = await echo.CreateClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Returns_The_Request_Body_Unchanged()
        {
            using var content = new StringContent("""{"hello":"Homeport ⚓"}""", Encoding.UTF8, "application/json");

            var response = await echo.CreateClient().PostAsync("/v1/echo", content);

            Assert.Equal("""{"hello":"Homeport ⚓"}""", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Returns_The_Request_Content_Type()
        {
            using var content = new StringContent("hello", Encoding.UTF8, "text/plain");

            var response = await echo.CreateClient().PostAsync("/v1/echo", content);

            Assert.Equal("text/plain; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }

        [Fact]
        public async Task Returns_The_Received_Headers()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/request");
            request.Headers.Add("X-Probe", "anchor");

            var response = await echo.CreateClient().SendAsync(request);

            var headers = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("headers");
            Assert.Equal("anchor", headers.GetProperty("x-probe").GetString());
        }

        [Fact]
        public async Task Leaves_Out_The_Authorization_Header()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/v1/request");
            request.Headers.Add("Authorization", "Bearer secret");

            var response = await echo.CreateClient().SendAsync(request);

            var headers = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("headers");
            Assert.False(headers.TryGetProperty("authorization", out _));
        }
    }
}
