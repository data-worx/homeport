// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Text.Json;
using Homeport.Sdk.Tests.Integration.Controllers;
using Microsoft.AspNetCore.TestHost;

namespace Homeport.Sdk.Tests.Integration;

public sealed class ResultExtensionsTests
{
    public sealed class ToActionResult
    {
        [Fact]
        public async Task Answers_A_Success_With_Its_Value()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(ThingsController.LookupPath + "box");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("box", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Answers_A_Failure_With_The_Mapped_Problem_Type()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(ThingsController.LookupPath + ThingsController.MissingName);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal("https://things.example/problems/not-found", root.GetProperty("type").GetString());
        }

        [Fact]
        public async Task Answers_A_Failure_With_The_Error_Message_As_Detail()
        {
            await using var app = await TestModule.StartAsync();

            var response = await app.GetTestClient().GetAsync(ThingsController.LookupPath + ThingsController.MissingName);

            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal("No thing is named 'missing'.", root.GetProperty("detail").GetString());
        }
    }
}
