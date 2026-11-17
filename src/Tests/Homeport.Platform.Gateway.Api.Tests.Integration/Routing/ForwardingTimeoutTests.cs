// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json.Nodes;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Routing;

public sealed class ForwardingTimeoutTests
{
    public sealed class ConfigureClusterAsync(TimingOutGateway gateway) : IClassFixture<TimingOutGateway>
    {
        private const string ModuleTimeoutType = "https://homeport.data-worx.de/problems/module-timeout";

        [Fact]
        public async Task Answers_A_Module_That_Does_Not_Answer_In_Time_As_Module_Timeout_Problem()
        {
            using var response = await gateway.CreateClient().GetAsync(TimingOutGateway.ModulePath);

            await AssertTimeoutProblemAsync(response);
        }

        [Fact]
        public async Task Answers_An_Admin_Service_That_Does_Not_Answer_In_Time_As_Module_Timeout_Problem()
        {
            using var response = await gateway.CreateAdminClient().GetAsync(TimingOutGateway.AdminPath);

            await AssertTimeoutProblemAsync(response);
        }

        private static async Task AssertTimeoutProblemAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.GatewayTimeout, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.ProblemJson, response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
            Assert.Equal(ModuleTimeoutType, problem?["type"]?.GetValue<string>());
        }
    }
}
