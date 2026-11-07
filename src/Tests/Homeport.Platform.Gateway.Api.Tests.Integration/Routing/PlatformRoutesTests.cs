// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.IdentityModel.JsonWebTokens;
using Wolverine.Tracking;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Routing;

public sealed class PlatformRoutesTests
{
    // Each test sets the bucket it draws from; the tests of one class run one after another.
    public sealed class Build(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private const string UsagePath = "/platform/v1/usage";

        [Fact]
        public async Task Forwards_The_Usage_Export_To_Metering_Below_The_Platform_Prefix()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Full;

            var forwarded = await gateway.CreateClient().GetFromJsonAsync<ForwardedRequest>(UsagePath);

            Assert.Equal("/v1/usage", forwarded!.Path);
        }

        [Fact]
        public async Task Replaces_The_Client_Token_With_A_Caller_Token_For_Metering()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Full;

            var forwarded = await gateway.CreateClient().GetFromJsonAsync<ForwardedRequest>(UsagePath);

            var token = new JsonWebToken(AuthenticationHeaderValue.Parse(forwarded!.Headers["authorization"]).Parameter);
            Assert.Equal(("homeport-gateway", MeteringService.Audience), (token.Issuer, Assert.Single(token.Audiences)));
        }

        [Fact]
        public async Task Names_Tenant_Client_Subject_And_Scopes_In_The_Caller_Token()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Full;

            var forwarded = await gateway.CreateClient().GetFromJsonAsync<ForwardedRequest>(UsagePath);

            var token = new JsonWebToken(AuthenticationHeaderValue.Parse(forwarded!.Headers["authorization"]).Parameter);
            Assert.Equal(
                (StandInControlPlane.KnownTenantId.ToString(), StandInControlPlane.KnownClientId, StandInIdentityProvider.SubjectOf(StandInControlPlane.KnownClientId), StandInIdentityProvider.DefaultScopes),
                (token.GetClaim("tenant_id").Value, token.GetClaim("client_id").Value, token.Subject, token.GetClaim("scope").Value));
        }

        [Fact]
        public async Task Answers_A_Client_Without_Usage_Read_As_Forbidden()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Full;

            using var response = await gateway.CreateAnonymousClient()
                .WithToken(gateway.IdentityProvider.Issue(StandInControlPlane.KnownClientId, scopes: "echo.invoke"))
                .GetAsync(UsagePath);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Answers_A_Request_Without_A_Token_As_Unauthorized()
        {
            using var response = await gateway.CreateAnonymousClient().GetAsync(UsagePath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Throttles_The_Usage_Export_Like_A_Module_Request()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Empty;

            using var response = await gateway.CreateClient().GetAsync(UsagePath);

            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        [Fact]
        public async Task Does_Not_Meter_The_Usage_Export()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Full;

            // Tracking returns once the gateway has handed off everything the request made it publish.
            var session = await gateway.Host.TrackActivity()
                .ExecuteAndWaitAsync(async Task (_) =>
                {
                    using var response = await gateway.CreateClient().GetAsync(UsagePath);
                    response.EnsureSuccessStatusCode();
                });

            Assert.Empty(session.Sent.MessagesOf<UsageRecorded>());
        }
    }
}
