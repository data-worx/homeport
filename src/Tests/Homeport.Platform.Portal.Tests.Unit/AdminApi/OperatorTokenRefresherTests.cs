// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Homeport.Platform.Portal.AdminApi;

namespace Homeport.Platform.Portal.Tests.Unit.AdminApi;

public sealed class OperatorTokenRefresherTests
{
    public sealed class RefreshAsync
    {
        private readonly StandInIdentityProvider identityProvider = new();

        [Fact]
        public async Task Returns_The_New_Tokens_And_When_The_Access_Token_Expires()
        {
            var tokens = await identityProvider.Refresher().RefreshAsync("refresh-token", CancellationToken.None);

            Assert.Equal(
                new OperatorTokenSet(StandInIdentityProvider.NewAccessToken, StandInIdentityProvider.NewRefreshToken, identityProvider.Clock.GetUtcNow() + StandInIdentityProvider.Lifetime),
                tokens);
        }

        [Fact]
        public async Task Sends_The_Refresh_Token_As_The_Portals_Client()
        {
            await identityProvider.Refresher().RefreshAsync("refresh-token", CancellationToken.None);

            var (request, body) = Assert.Single(identityProvider.Handler.Received);
            Assert.Equal(StandInIdentityProvider.TokenEndpoint, request.RequestUri!.ToString());
            Assert.Equal(
                $"grant_type=refresh_token&refresh_token=refresh-token&client_id={StandInIdentityProvider.ClientId}&client_secret={StandInIdentityProvider.ClientSecret}",
                body);
        }

        [Fact]
        public async Task Returns_Nothing_When_The_Identity_Provider_Refuses_The_Refresh_Token()
        {
            identityProvider.Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.BadRequest);

            var tokens = await identityProvider.Refresher().RefreshAsync("expired-refresh-token", CancellationToken.None);

            Assert.Null(tokens);
        }

        [Fact]
        public async Task Returns_Nothing_Without_A_Refresh_Token()
        {
            var tokens = await identityProvider.Refresher().RefreshAsync(null, CancellationToken.None);

            Assert.Null(tokens);
            Assert.Empty(identityProvider.Handler.Received);
        }
    }
}
