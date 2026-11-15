// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using Homeport.Shared.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;

namespace Homeport.Shared.Internal.Authorization.Tests.Unit;

public sealed class AdminHandlerTests
{
    public sealed class HandleAsync
    {
        [Fact]
        public async Task Succeeds_For_The_Operators_Caller_Token()
        {
            var context = await RunAsync(Tokens.OperatorToken());

            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task Fails_For_A_Token_With_A_Tenant()
        {
            var context = await RunAsync(Tokens.With(
                (GatewayTokens.SubjectClaim, Tokens.Operator),
                (GatewayTokens.ScopeClaim, GatewayTokens.AdminScope),
                (GatewayTokens.TenantIdClaim, Guid.NewGuid().ToString())));

            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task Fails_For_A_Token_Without_The_Admin_Scope()
        {
            var context = await RunAsync(Tokens.With(
                (GatewayTokens.SubjectClaim, Tokens.Operator),
                (GatewayTokens.ScopeClaim, "echo:read")));

            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task Fails_For_A_Token_Without_A_Subject()
        {
            var context = await RunAsync(Tokens.With((GatewayTokens.ScopeClaim, GatewayTokens.AdminScope)));

            Assert.True(context.HasFailed);
        }

        [Fact]
        public async Task Fails_For_The_Gateways_Service_Token()
        {
            var context = await RunAsync(Tokens.ServiceToken());

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
            var context = new AuthorizationHandlerContext([new AdminRequirement()], user, resource: null);
            await new AdminHandler(NullLogger<AdminHandler>.Instance).HandleAsync(context);
            return context;
        }
    }
}
