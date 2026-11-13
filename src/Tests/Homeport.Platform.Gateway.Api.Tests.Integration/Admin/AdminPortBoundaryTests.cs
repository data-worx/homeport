// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Admin;

public sealed class AdminPortBoundaryTests
{
    public sealed class InvokeAsync(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private static readonly string[] AdminRoles = [StandInIdentityProvider.AdminRole];

        [Fact]
        public async Task Answers_An_Admin_Path_On_The_Public_Port_Like_An_Unknown_Path()
        {
            var client = gateway.CreateAnonymousClient().WithToken(gateway.IdentityProvider.IssueOperator(AdminRoles));

            var admin = await AnswerAsync(client, ForwardingGateway.AdminPath);
            var unknown = await AnswerAsync(client, "/platform/v1/unknown");

            Assert.Equal(unknown, admin);
        }

        [Fact]
        public async Task Answers_An_Admin_Path_On_The_Public_Port_As_Not_Found_With_A_Client_Token()
        {
            using var response = await gateway.CreateClient().GetAsync(ForwardingGateway.AdminPath);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_A_Module_Path_On_The_Admin_Port_As_Not_Found()
        {
            using var response = await gateway.CreateAdminClient(gateway.IdentityProvider.IssueOperator(AdminRoles))
                .GetAsync("/modules/echo" + ForwardingGateway.RequestPath);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_A_Platform_Path_On_The_Admin_Port_As_Not_Found()
        {
            using var response = await gateway.CreateAdminClient(gateway.IdentityProvider.Issue(StandInControlPlane.KnownClientId))
                .GetAsync("/platform/v1/usage");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        // Everything a caller can tell the two answers apart by, but the trace id.
        private static async Task<(HttpStatusCode Status, string? MediaType, string? Type, string? Title)> AnswerAsync(HttpClient client, string path)
        {
            using var response = await client.GetAsync(path);
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();

            return (response.StatusCode, response.Content.Headers.ContentType?.MediaType, problem?.Type?.ToString(), problem?.Title);
        }
    }
}
