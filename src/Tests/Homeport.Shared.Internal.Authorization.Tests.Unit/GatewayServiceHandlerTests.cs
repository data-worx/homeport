// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Homeport.Shared.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Homeport.Shared.Internal.Authorization.Tests.Unit;

public sealed class GatewayServiceHandlerTests
{
    public sealed class HandleAsync
    {
        [Fact]
        public async Task Succeeds_For_The_Gateways_Service_Token()
        {
            var context = await RunAsync(Tokens.ServiceToken());

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task Fails_For_Another_Subject()
        {
            var context = await RunAsync(Tokens.With((GatewayTokens.SubjectClaim, "billing-service")));

            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task Fails_For_A_Token_With_A_Tenant()
        {
            var context = await RunAsync(Tokens.With(
                (GatewayTokens.SubjectClaim, GatewayTokens.GatewaySubject),
                (GatewayTokens.TenantIdClaim, Guid.NewGuid().ToString())));

            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task Fails_For_The_Operators_Caller_Token()
        {
            var context = await RunAsync(Tokens.With(
                (GatewayTokens.SubjectClaim, GatewayTokens.GatewaySubject),
                (GatewayTokens.ScopeClaim, GatewayTokens.AdminScope)));

            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task Leaves_An_Anonymous_Request_To_Be_Challenged()
        {
            var context = await RunAsync(Tokens.Anonymous);

            Assert.False(context.HasSucceeded || context.HasFailed);
        }

        private static async Task<AuthorizationHandlerContext> RunAsync(ClaimsPrincipal user)
        {
            var context = new AuthorizationHandlerContext([new GatewayServiceRequirement()], user, resource: null);
            await new GatewayServiceHandler(NullLogger<GatewayServiceHandler>.Instance).HandleAsync(context);
            return context;
        }
    }
}
