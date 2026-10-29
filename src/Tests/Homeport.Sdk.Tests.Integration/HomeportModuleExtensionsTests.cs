// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Homeport.Sdk.Tests.Integration.Controllers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

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

        [Fact]
        public async Task Answers_An_Unhandled_Exception_As_An_Internal_Problem()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(ThingsController.ThrowPath);

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal("https://homeport.data-worx.de/problems/internal", root.GetProperty("type").GetString());
            Assert.Equal(500, root.GetProperty("status").GetInt32());
        }

        [Fact]
        public async Task Answers_A_Problem_With_The_W3C_Trace_Id()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(ThingsController.ThrowPath);

            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Matches("^[0-9a-f]{32}$", root.GetProperty("traceId").GetString());
        }

        [Fact]
        public async Task Leaves_The_Exception_Out_Of_An_Internal_Problem()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(ThingsController.ThrowPath);

            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("secret-db", body);
            Assert.DoesNotContain(nameof(InvalidOperationException), body);
            Assert.Equal(
                ["status", "title", "traceId", "type"],
                JsonDocument.Parse(body).RootElement.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task Logs_An_Unhandled_Exception_Once_At_Error()
        {
            using var logs = new CollectingLoggerProvider();
            await using var app = await TestModule.StartAsync(logs);

            await app.GetTestClient().GetAsync(ThingsController.ThrowPath);

            var logged = Assert.Single(logs.Entries, entry => entry.Exception is not null);
            Assert.Equal(LogLevel.Error, logged.Level);
            Assert.Equal(ThingsController.ExceptionMessage, logged.Exception!.Message);
        }

        [Fact]
        public async Task Answers_An_Invalid_Request_As_A_Validation_Problem()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().PostAsJsonAsync(ThingsController.CreatePath, new { name = "box", items = new[] { new { } } });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal("https://homeport.data-worx.de/problems/validation", root.GetProperty("type").GetString());
            Assert.Equal(400, root.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrEmpty(root.GetProperty("traceId").GetString()));
        }

        [Fact]
        public async Task Keys_Validation_Errors_By_CamelCase_Field_Path()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().PostAsJsonAsync(ThingsController.CreatePath, new { items = new[] { new { } } });

            var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
            Assert.Equal(
                ["items[0].name", "name"],
                errors.EnumerateObject().Select(property => property.Name).Order(StringComparer.Ordinal));
        }

        [Fact]
        public async Task Keys_An_Unreadable_Field_By_Its_CamelCase_Field_Path()
        {
            await using var app = await TestModule.StartAsync();
            using var content = new StringContent("""{"name":"box","items":[{"name":7}]}""", Encoding.UTF8, "application/json");

            var response = await app.GetTestClient().PostAsync(ThingsController.CreatePath, content);

            var errors = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("errors");
            Assert.Contains("items[0].name", errors.EnumerateObject().Select(property => property.Name));
        }

        [Theory]
        [InlineData("401")]
        [InlineData("403")]
        [InlineData("429")]
        [InlineData("500")]
        public async Task Lists_A_Common_Error_As_Problem_Details_On_Every_Operation(string status)
        {
            var paths = await OpenApiPaths();

            var operations = paths.EnumerateObject().SelectMany(path => path.Value.EnumerateObject()).ToList();
            Assert.NotEmpty(operations);
            Assert.All(operations, operation => Assert.True(
                operation.Value.GetProperty("responses").GetProperty(status).GetProperty("content")
                    .TryGetProperty("application/problem+json", out _)));
        }

        [Fact]
        public async Task Keeps_An_Error_The_Action_Declares_Itself()
        {
            var paths = await OpenApiPaths();

            var response = paths.GetProperty(ThingsController.ThrowPath).GetProperty("get")
                .GetProperty("responses").GetProperty("500");
            Assert.Equal(ThingsController.ThrowFailureDescription, response.GetProperty("description").GetString());
        }

        private static async Task<JsonElement> OpenApiPaths()
        {
            await using var app = await TestModule.StartAsync();
            var document = await app.GetTestClient().GetStringAsync(ModuleManifest.OpenApiPath);

            return JsonDocument.Parse(document).RootElement.GetProperty("paths").Clone();
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

        [Fact]
        public async Task Answers_An_Unknown_Path_As_A_Problem()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync("/v1/nowhere");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(404, root.GetProperty("status").GetInt32());
            Assert.False(string.IsNullOrEmpty(root.GetProperty("traceId").GetString()));
        }
    }
}
