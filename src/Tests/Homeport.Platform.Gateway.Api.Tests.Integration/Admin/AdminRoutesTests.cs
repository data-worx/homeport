// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Homeport.Platform.Gateway.Api.Admin;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Admin;

public sealed class AdminRoutesTests
{
    public sealed class Build(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        // Among others, as an identity provider lists every role of the user.
        private static readonly string[] AdminRoles = ["offline_access", StandInIdentityProvider.AdminRole];

        [Fact]
        public async Task Forwards_An_Admin_Request_On_The_Admin_Port_Below_The_Platform_Prefix()
        {
            var forwarded = await AdminClient().GetFromJsonAsync<ForwardedRequest>(ForwardingGateway.AdminPath);

            Assert.Equal(ForwardingGateway.AdminServicePath, forwarded!.Path);
        }

        [Fact]
        public async Task Replaces_The_Operator_Token_With_A_Caller_Token_For_The_Service()
        {
            var token = await ForwardedTokenAsync(AdminClient());

            Assert.Equal(("homeport-gateway", ForwardingGateway.AdminServiceAudience), (token.Issuer, Assert.Single(token.Audiences)));
        }

        [Fact]
        public async Task Names_The_Operators_Subject_And_Name_And_The_Admin_Scope_In_The_Caller_Token()
        {
            var token = await ForwardedTokenAsync(AdminClient());

            Assert.Equal(
                (StandInIdentityProvider.OperatorSubject, StandInIdentityProvider.OperatorName, "admin"),
                (token.Subject, token.GetClaim("name").Value, token.GetClaim("scope").Value));
        }

        [Fact]
        public async Task Names_Neither_Tenant_Nor_Client_In_The_Caller_Token()
        {
            var token = await ForwardedTokenAsync(AdminClient());

            Assert.False(token.TryGetClaim("tenant_id", out _) || token.TryGetClaim("client_id", out _));
        }

        [Fact]
        public async Task Names_The_Operator_By_Subject_When_The_Token_Carries_No_Name()
        {
            var token = await ForwardedTokenAsync(gateway.CreateAdminClient(gateway.IdentityProvider.IssueOperator(AdminRoles, withName: false)));

            Assert.Equal(StandInIdentityProvider.OperatorSubject, token.GetClaim("name").Value);
        }

        [Fact]
        public async Task Does_Not_Throttle_An_Admin_Request()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Empty;

            using var response = await AdminClient().GetAsync(ForwardingGateway.AdminPath);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public void Refuses_An_Endpoint_Outside_The_Admin_Prefix()
        {
            var routes = new AdminRoutes([new AdminEndpoint(HttpMethods.Get, "/v1/usage", new AdminService("metering", "homeport-metering", new Uri("http://metering")))]);

            Assert.Throws<InvalidOperationException>(() => routes.Build());
        }

        private HttpClient AdminClient() =>
            gateway.CreateAdminClient(gateway.IdentityProvider.IssueOperator(AdminRoles));

        private static async Task<JsonWebToken> ForwardedTokenAsync(HttpClient client)
        {
            var forwarded = await client.GetFromJsonAsync<ForwardedRequest>(ForwardingGateway.AdminPath);

            return new JsonWebToken(AuthenticationHeaderValue.Parse(forwarded!.Headers["authorization"]).Parameter);
        }
    }
}
