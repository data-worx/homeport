// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;

namespace Homeport.Sdk.Tests.Integration;

public sealed class HomeportModuleExtensionsTests
{
    public sealed class AddHomeportModule
    {
        [Fact]
        public void Refuses_A_Manifest_That_Breaks_The_Contract()
        {
            var manifest = TestModule.Manifest with { Id = "platform" };

            var exception = Assert.Throws<InvalidOperationException>(() => TestModule.CreateBuilder(manifest));

            Assert.Contains("Id 'platform' is reserved.", exception.Message);
        }
    }

    public sealed class MapHomeportModule
    {
        [Fact]
        public async Task Serves_The_Manifest_With_Every_Contract_Field()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetStringAsync(ModuleManifest.ManifestPath);

            var root = JsonDocument.Parse(response).RootElement;
            Assert.Equal(
                ["contractVersion", "id", "meters", "name", "openApiUrl", "routes", "scopes", "version"],
                root.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task Serves_The_Declared_Manifest()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetStringAsync(ModuleManifest.ManifestPath);

            var root = JsonDocument.Parse(response).RootElement;
            Assert.Equal("things", root.GetProperty("id").GetString());
            Assert.Equal("Things", root.GetProperty("name").GetString());
            Assert.Equal("1.2.3", root.GetProperty("version").GetString());
            Assert.Equal(0, root.GetProperty("contractVersion").GetInt32());
            Assert.Equal(["things.write"], root.GetProperty("scopes").EnumerateArray().Select(scope => scope.GetString()));
            Assert.Equal(["requests"], root.GetProperty("meters").EnumerateArray().Select(meter => meter.GetString()));
        }

        [Fact]
        public async Task Serves_Every_Field_Of_A_Route()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetStringAsync(ModuleManifest.ManifestPath);

            var route = JsonDocument.Parse(response).RootElement.GetProperty("routes")[0];
            Assert.Equal("POST", route.GetProperty("method").GetString());
            Assert.Equal("/v1/things", route.GetProperty("path").GetString());
            Assert.Equal("things.write", route.GetProperty("scope").GetString());
            Assert.Equal("requests", route.GetProperty("meter").GetString());
            Assert.Equal(2, route.GetProperty("quantity").GetInt32());
        }

        [Fact]
        public async Task Serves_The_OpenApi_Document_At_The_Url_The_Manifest_Names()
        {
            await using var app = await TestModule.StartAsync();
            var client = app.GetTestClient();
            var manifest = JsonDocument.Parse(await client.GetStringAsync(ModuleManifest.ManifestPath)).RootElement;

            var response = await client.GetAsync(manifest.GetProperty("openApiUrl").GetString());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.TryGetProperty("openapi", out _));
        }

        [Theory]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public async Task Answers_Health(string path)
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
