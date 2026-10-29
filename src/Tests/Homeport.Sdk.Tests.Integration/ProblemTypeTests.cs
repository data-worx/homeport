// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Text.Json;
using Homeport.Sdk.Tests.Integration.Controllers;
using Microsoft.AspNetCore.TestHost;

namespace Homeport.Sdk.Tests.Integration;

public sealed class ProblemTypeTests
{
    public sealed class ToResult
    {
        [Fact]
        public async Task Answers_With_The_Declared_Type_Title_And_Status()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(ThingsController.ConflictPath);

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal("https://things.example/problems/conflict", root.GetProperty("type").GetString());
            Assert.Equal("The thing already exists.", root.GetProperty("title").GetString());
            Assert.Equal(409, root.GetProperty("status").GetInt32());
        }

        [Fact]
        public async Task Answers_With_The_Detail_And_A_Trace_Id()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(ThingsController.ConflictPath);

            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(ThingsController.ConflictDetail, root.GetProperty("detail").GetString());
            Assert.False(string.IsNullOrEmpty(root.GetProperty("traceId").GetString()));
        }

        [Fact]
        public async Task Answers_As_Problem_Json()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(ThingsController.ConflictPath);

            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }
    }
}
