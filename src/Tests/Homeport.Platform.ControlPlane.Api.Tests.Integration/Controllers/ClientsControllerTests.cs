// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Shared.Client.Problems;
using Homeport.Shared.Internal.Platform.ControlPlane;

namespace Homeport.Platform.ControlPlane.Api.Tests.Integration.Controllers;

public sealed class ClientsControllerTests
{
    public sealed class Get(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Tenant_And_Plan_Of_A_Known_Client()
        {
            var client = await controlPlane.CreateGatewayClient().GetFromJsonAsync<ClientResponse>(PathOf(RunningControlPlane.ClientId));

            var plan = RunningControlPlane.Plan;
            Assert.Equal(
                new ClientResponse(RunningControlPlane.ClientId, RunningControlPlane.Tenant.Id, new PlanResponse(plan.Id, plan.RequestsPerSecond, plan.Burst)),
                client);
        }

        [Fact]
        public async Task Answers_Unauthorized_Without_A_Service_Token()
        {
            var response = await controlPlane.CreateClient().GetAsync(PathOf(RunningControlPlane.ClientId));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(new Uri(ProblemConventions.TypePrefix + "unauthorized"), problem?.Type);
        }

        [Fact]
        public async Task Answers_Not_Found_For_An_Unknown_Client()
        {
            var response = await controlPlane.CreateGatewayClient().GetAsync(PathOf("unknown-client"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static string PathOf(string clientId)
        {
            return ControlPlaneService.ClientPath.Replace(
                $"{{{ControlPlaneService.ClientIdRouteValue}}}",
                Uri.EscapeDataString(clientId),
                StringComparison.Ordinal);
        }
    }
}
