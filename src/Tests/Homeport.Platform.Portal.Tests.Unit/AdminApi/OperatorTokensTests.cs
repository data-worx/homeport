// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Homeport.Platform.Portal.AdminApi;
using Microsoft.AspNetCore.Http;

namespace Homeport.Platform.Portal.Tests.Unit.AdminApi;

public sealed class OperatorTokensTests
{
    public sealed class AccessTokenAsync
    {
        private readonly StandInIdentityProvider identityProvider = new();

        [Fact]
        public async Task Returns_The_Access_Token_Of_The_Cookie_While_It_Is_Valid()
        {
            using var tokens = TokensOf(new SignedInOperator("cookie-token", "refresh-token", identityProvider.Clock.GetUtcNow().AddMinutes(4)));

            var accessToken = await tokens.AccessTokenAsync(CancellationToken.None);

            Assert.Equal("cookie-token", accessToken);
            Assert.Empty(identityProvider.Handler.Received);
        }

        [Fact]
        public async Task Reads_The_Cookie_Once_Per_Circuit()
        {
            var signedIn = new SignedInOperator("cookie-token", "refresh-token", identityProvider.Clock.GetUtcNow().AddMinutes(4));
            using var tokens = TokensOf(signedIn);

            await tokens.AccessTokenAsync(CancellationToken.None);
            await tokens.AccessTokenAsync(CancellationToken.None);

            Assert.Equal(1, signedIn.Reads);
        }

        [Fact]
        public async Task Renews_The_Access_Token_Shortly_Before_It_Expires()
        {
            var expiresAt = identityProvider.Clock.GetUtcNow().AddMinutes(4);
            using var tokens = TokensOf(new SignedInOperator("cookie-token", "refresh-token", expiresAt));
            await tokens.AccessTokenAsync(CancellationToken.None);
            identityProvider.Clock.SetUtcNow(expiresAt - OperatorTokens.RenewalMargin);

            var accessToken = await tokens.AccessTokenAsync(CancellationToken.None);

            Assert.Equal(StandInIdentityProvider.NewAccessToken, accessToken);
        }

        [Fact]
        public async Task Keeps_The_Renewed_Access_Token_Until_It_Expires_In_Turn()
        {
            var expiresAt = identityProvider.Clock.GetUtcNow().AddMinutes(4);
            using var tokens = TokensOf(new SignedInOperator("cookie-token", "refresh-token", expiresAt));
            identityProvider.Clock.SetUtcNow(expiresAt);
            await tokens.AccessTokenAsync(CancellationToken.None);

            await tokens.AccessTokenAsync(CancellationToken.None);

            Assert.Single(identityProvider.Handler.Received);
        }

        [Fact]
        public async Task Returns_Nothing_When_The_Renewal_Is_Refused()
        {
            var expiresAt = identityProvider.Clock.GetUtcNow().AddMinutes(4);
            using var tokens = TokensOf(new SignedInOperator("cookie-token", "refresh-token", expiresAt));
            identityProvider.Clock.SetUtcNow(expiresAt);
            identityProvider.Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.BadRequest);

            var accessToken = await tokens.AccessTokenAsync(CancellationToken.None);

            Assert.Null(accessToken);
        }

        [Fact]
        public async Task Returns_Nothing_Without_The_Request_That_Opened_The_Circuit()
        {
            using var tokens = TokensOf(new HttpContextAccessor());

            var accessToken = await tokens.AccessTokenAsync(CancellationToken.None);

            Assert.Null(accessToken);
        }

        private OperatorTokens TokensOf(IHttpContextAccessor accessor) => new(accessor, identityProvider.Refresher(), identityProvider.Clock);
    }
}
