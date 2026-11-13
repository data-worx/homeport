// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Admin;

public sealed class AdminClaimHandlerTests
{
    public sealed class HandleRequirementAsync(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        [Fact]
        public async Task Answers_A_Request_Without_A_Token_As_Unauthorized_Problem()
        {
            using var response = await gateway.CreateAdminClient().GetAsync(ForwardingGateway.AdminPath);

            Assert.Equal(
                (HttpStatusCode.Unauthorized, MediaTypeNames.Application.ProblemJson, "https://homeport.data-worx.de/problems/unauthorized"),
                (response.StatusCode, response.Content.Headers.ContentType?.MediaType, await TypeAsync(response)));
        }

        [Fact]
        public async Task Answers_A_Client_Token_As_Unauthorized()
        {
            using var response = await gateway.CreateAdminClient(gateway.IdentityProvider.Issue(StandInControlPlane.KnownClientId))
                .GetAsync(ForwardingGateway.AdminPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Answers_An_Operator_Token_Without_The_Admin_Claim_As_Admin_Claim_Missing_Problem()
        {
            using var response = await gateway.CreateAdminClient(gateway.IdentityProvider.IssueOperator(["offline_access"]))
                .GetAsync(ForwardingGateway.AdminPath);

            Assert.Equal(
                (HttpStatusCode.Forbidden, MediaTypeNames.Application.ProblemJson, "https://homeport.data-worx.de/problems/admin-claim-missing"),
                (response.StatusCode, response.Content.Headers.ContentType?.MediaType, await TypeAsync(response)));
        }

        private static async Task<string?> TypeAsync(HttpResponseMessage response) =>
            (await response.Content.ReadFromJsonAsync<ProblemResponse>())?.Type?.ToString();
    }
}
