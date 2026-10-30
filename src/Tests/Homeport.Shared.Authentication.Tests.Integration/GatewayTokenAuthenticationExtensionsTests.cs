// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Shared.Authentication.Tests.Integration;

public sealed class GatewayTokenAuthenticationExtensionsTests
{
    public sealed class AddGatewayTokenAuthentication : IDisposable
    {
        private static readonly TimeSpan SettleTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan SettlePoll = TimeSpan.FromMilliseconds(50);

        private readonly TestKey current = new("current");
        private readonly TestKey next = new("next");

        [Fact]
        public async Task Answers_Unauthorized_Without_A_Token()
        {
            await using var service = await ValidatingService.StartAsync(current);

            var response = await service.CreateClient().GetAsync(ValidatingService.ProtectedPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Accepts_A_Token_Signed_With_A_Key_From_The_Jwks()
        {
            await using var service = await ValidatingService.StartAsync(current);

            var response = await Send(service, current.Sign(ValidatingService.Audience));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Unauthorized_For_A_Token_For_Another_Audience()
        {
            await using var service = await ValidatingService.StartAsync(current);

            var response = await Send(service, current.Sign("homeport-control-plane"));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Rejects_The_First_Token_With_A_Key_It_Has_Not_Read_Yet()
        {
            await using var service = await ValidatingService.StartAsync(current);
            await Send(service, current.Sign(ValidatingService.Audience));
            service.Publish(next, current);

            var response = await Send(service, next.Sign(ValidatingService.Audience));

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Accepts_A_Key_Published_After_The_Jwks_Was_Read_Once_It_Has_Refreshed()
        {
            await using var service = await ValidatingService.StartAsync(current);
            await Send(service, current.Sign(ValidatingService.Audience));
            service.Publish(next, current);
            await Send(service, next.Sign(ValidatingService.Audience));

            var status = await StatusOnceSettled(service, next.Sign(ValidatingService.Audience));

            Assert.Equal(HttpStatusCode.OK, status);
        }

        [Fact]
        public async Task Logs_A_Rejected_Token_At_Warning_With_The_Reason_And_Without_The_Token()
        {
            await using var service = await ValidatingService.StartAsync(current);
            var token = current.Sign("homeport-control-plane");

            await Send(service, token);

            var logged = Assert.Single(service.Logs.GetSnapshot(), entry => entry.Category == "Homeport.Shared.Authentication.GatewayTokenEvents");
            Assert.Equal(LogLevel.Warning, logged.Level);
            Assert.Contains(nameof(SecurityTokenInvalidAudienceException), logged.Message);
            Assert.DoesNotContain(token, logged.Message);
        }

        [Fact]
        public async Task Logs_A_Failed_Jwks_Retrieval_At_Warning()
        {
            await using var service = await ValidatingService.StartAsync(current);
            service.Fail();

            await Send(service, current.Sign(ValidatingService.Audience));

            Assert.Contains(
                service.Logs.GetSnapshot(),
                entry => entry.Category == "Homeport.Shared.Authentication.JsonWebKeySetRetriever" && entry.Level == LogLevel.Warning);
        }

        public void Dispose()
        {
            current.Dispose();
            next.Dispose();
        }

        // The refresh a token with an unknown key triggers runs in the
        // background, so the service accepts the new key shortly afterwards.
        private static async Task<HttpStatusCode> StatusOnceSettled(ValidatingService service, string token)
        {
            using var timeout = new CancellationTokenSource(SettleTimeout);
            var response = await Send(service, token);
            while (response.StatusCode != HttpStatusCode.OK && !timeout.IsCancellationRequested)
            {
                await Task.Delay(SettlePoll);
                response = await Send(service, token);
            }

            return response.StatusCode;
        }

        private static async Task<HttpResponseMessage> Send(ValidatingService service, string token)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ValidatingService.ProtectedPath);
            request.Headers.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, token);

            return await service.CreateClient().SendAsync(request);
        }
    }
}
