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
    }
}
