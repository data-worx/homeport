// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Homeport.Platform.Portal.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.WebUtilities;

namespace Homeport.Platform.Portal.Tests.Integration.Controllers;

public sealed class AccountControllerTests
{
    public sealed class SignInOperator(PortalFactory portal) : IClassFixture<PortalFactory>
    {
        [Fact]
        public async Task Sends_The_Operator_To_The_Identity_Provider_With_A_Code_Challenge()
        {
            using var response = await portal.CreateClient(PortalFactory.HttpsAddress).GetAsync("/sign-in/start");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            var location = response.Headers.Location!;
            Assert.StartsWith(PortalFactory.AuthorizationEndpoint + "?", location.AbsoluteUri, StringComparison.Ordinal);
            var query = QueryHelpers.ParseQuery(location.Query);
            Assert.Equal(("code", "S256"), (query["response_type"].ToString(), query["code_challenge_method"].ToString()));
            Assert.False(string.IsNullOrEmpty(query["code_challenge"]));
        }

        [Fact]
        public async Task Refuses_A_Sign_In_Over_Plain_Http_Without_A_Trusted_Proxy()
        {
            // A proxy the portal does not trust cannot claim HTTPS for it.
            using var request = new HttpRequestMessage(HttpMethod.Get, "/sign-in/start");
            request.Headers.Add("X-Forwarded-Proto", Uri.UriSchemeHttps);

            using var response = await portal.CreateClient(PortalFactory.HttpAddress).SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/sign-in?reason=https-required", response.Headers.Location?.OriginalString);
        }

        [Theory]
        [InlineData("https://evil.example/")]
        [InlineData("//evil.example/")]
        public void Returns_Only_To_A_Page_Of_The_Portal(string returnUrl)
        {
            var controller = new AccountController();
            controller.Url = new UrlHelper(new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor()));

            var challenge = Assert.IsType<ChallengeResult>(controller.SignInOperator(returnUrl));

            Assert.Equal("/", challenge.Properties?.RedirectUri);
        }
    }
}
