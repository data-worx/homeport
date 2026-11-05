// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Mime;
using Microsoft.Net.Http.Headers;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Authentication;

public sealed class ModuleRequestChallengeTests
{
    public sealed class InvokeAsync(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private const string EchoPath = "/modules/echo" + ForwardingGateway.RequestPath;

        [Fact]
        public async Task Answers_An_Undeclared_Method_Without_A_Token_As_Unauthorized_Problem_Without_Allow()
        {
            using var response = await gateway.CreateAnonymousClient().PostAsync(EchoPath, content: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.ProblemJson, response.Content.Headers.ContentType?.MediaType);
            Assert.False(response.Content.Headers.Contains(HeaderNames.Allow));
        }

        [Fact]
        public async Task Answers_An_Undeclared_Method_With_A_Token_As_Method_Not_Allowed_Problem_With_Allow()
        {
            using var response = await gateway.CreateClient().PostAsync(EchoPath, content: null);

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.ProblemJson, response.Content.Headers.ContentType?.MediaType);
            Assert.Equal([HttpMethod.Get.Method], response.Content.Headers.Allow);
        }

        [Fact]
        public async Task Answers_An_Undeclared_Path_Without_A_Token_As_Unauthorized()
        {
            using var response = await gateway.CreateAnonymousClient().GetAsync("/modules/echo/v1/undeclared");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Answers_An_Unknown_Module_Without_A_Token_As_Unauthorized()
        {
            using var response = await gateway.CreateAnonymousClient().GetAsync("/modules/unknown/v1/request");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
