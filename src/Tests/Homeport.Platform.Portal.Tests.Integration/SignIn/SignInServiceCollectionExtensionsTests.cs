// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Homeport.Platform.Portal.SignIn;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Portal.Tests.Integration.SignIn;

public sealed class SignInServiceCollectionExtensionsTests
{
    public sealed class AddOperatorSignIn(PortalFactory portal) : IClassFixture<PortalFactory>
    {
        [Fact]
        public async Task Sends_An_Anonymous_Request_To_The_Sign_In_Page_Not_To_The_Identity_Provider()
        {
            using var response = await portal.CreateClient(PortalFactory.HttpsAddress).GetAsync("/");

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/sign-in", response.Headers.Location?.AbsolutePath);
        }

        [Fact]
        public async Task Sends_A_User_Without_The_Claim_To_The_Access_Denied_Page()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add(PortalFactory.TestUser.Header, PortalFactory.TestUser.WithoutRole);

            using var response = await portal.CreateClient(PortalFactory.HttpsAddress).SendAsync(request);

            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/access-denied", response.Headers.Location?.AbsolutePath);
        }

        [Fact]
        public void Sets_The_Session_Cookie_Secure_HttpOnly_And_Lax()
        {
            var cookie = portal.Services.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
                .Get(CookieAuthenticationDefaults.AuthenticationScheme).Cookie;

            Assert.Equal((CookieSecurePolicy.Always, true, SameSiteMode.Lax), (cookie.SecurePolicy, cookie.HttpOnly, cookie.SameSite));
        }

        [Fact]
        public void Sets_The_Sign_In_Cookies_Secure_And_Lax()
        {
            var oidc = portal.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                .Get(OpenIdConnectDefaults.AuthenticationScheme);

            Assert.Equal(
                (CookieSecurePolicy.Always, SameSiteMode.Lax, CookieSecurePolicy.Always, SameSiteMode.Lax),
                (oidc.CorrelationCookie.SecurePolicy, oidc.CorrelationCookie.SameSite, oidc.NonceCookie.SecurePolicy, oidc.NonceCookie.SameSite));
        }

        [Fact]
        public void Takes_The_Name_From_The_Configured_Claim()
        {
            var oidc = portal.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                .Get(OpenIdConnectDefaults.AuthenticationScheme);

            Assert.Equal(SignInOptions.DefaultNameClaim, oidc.TokenValidationParameters.NameClaimType);
        }
    }
}
