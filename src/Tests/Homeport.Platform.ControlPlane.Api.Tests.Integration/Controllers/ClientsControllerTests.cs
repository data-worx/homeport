// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Client.Problems;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Microsoft.Extensions.Logging;

namespace Homeport.Platform.ControlPlane.Api.Tests.Integration.Controllers;

public sealed class ClientsControllerTests
{
    private static readonly Uri InsufficientScopeType = new(ProblemConventions.TypePrefix + "insufficient-scope");

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
        public async Task Answers_Forbidden_With_The_Operators_Caller_Token()
        {
            var response = await controlPlane.CreateOperatorClient().GetAsync(PathOf(RunningControlPlane.ClientId));

            await AssertForbiddenAsync(response);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Clients_Caller_Token()
        {
            var response = await controlPlane.CreateCallerClient(UsageApi.ReadScope).GetAsync(PathOf(RunningControlPlane.ClientId));

            await AssertForbiddenAsync(response);
        }

        [Fact]
        public async Task Logs_A_Refused_Caller_Token_At_Warning_With_The_Reason_And_Without_The_Token()
        {
            using var client = controlPlane.CreateOperatorClient();
            var token = client.DefaultRequestHeaders.Authorization!.Parameter!;

            await client.GetAsync(PathOf(RunningControlPlane.ClientId));

            var logged = controlPlane.Logs.GetSnapshot();
            Assert.Contains(
                logged,
                entry => entry.Category == "Homeport.Shared.Internal.Authorization.GatewayServiceHandler"
                    && entry.Level == LogLevel.Warning
                    && entry.Message.Contains("subject is not the gateway", StringComparison.Ordinal));
            Assert.DoesNotContain(logged, entry => entry.Message.Contains(token, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Answers_Not_Found_For_An_Unknown_Client()
        {
            var response = await controlPlane.CreateGatewayClient().GetAsync(PathOf("unknown-client"));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static async Task AssertForbiddenAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(InsufficientScopeType, problem?.Type);
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
