// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Homeport.Platform.Portal.Tests.Integration;

/// <summary>
/// The portal with an identity provider it never calls: its endpoints are
/// configured, not fetched. A request names its signed-in user in
/// <see cref="TestUser.Header"/>; without it, the request is anonymous.
/// </summary>
public sealed class PortalFactory : WebApplicationFactory<Program>
{
    internal const string Authority = "https://idp.example/realms/homeport";

    internal const string AuthorizationEndpoint = Authority + "/protocol/openid-connect/auth";

    internal static readonly Uri HttpsAddress = new("https://localhost");

    internal static readonly Uri HttpAddress = new("http://localhost");

    /// <summary>A client that does not follow redirects, so a test sees where the portal sends the browser.</summary>
    internal HttpClient CreateClient(Uri baseAddress) =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = baseAddress, AllowAutoRedirect = false });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("SignIn:Authority", Authority);
        builder.UseSetting("SignIn:ClientId", "portal");
        builder.UseSetting("SignIn:ClientSecret", "test-secret");
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureTestServices(services =>
        {
            services.PostConfigure<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme, oidc =>
            {
                var configuration = new OpenIdConnectConfiguration
                {
                    Issuer = Authority,
                    AuthorizationEndpoint = AuthorizationEndpoint,
                    TokenEndpoint = Authority + "/protocol/openid-connect/token",
                    EndSessionEndpoint = Authority + "/protocol/openid-connect/logout",
                };
                oidc.Configuration = configuration;
                oidc.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
            });

            // Authenticates from the header; challenge and forbid stay with
            // the portal's cookie scheme.
            services.AddAuthentication()
                .AddScheme<AuthenticationSchemeOptions, TestUser>(TestUser.SchemeName, configureOptions: null);
            services.PostConfigure<AuthenticationOptions>(options => options.DefaultAuthenticateScheme = TestUser.SchemeName);
        });
    }

    /// <summary>Signs a request in as the user its header names.</summary>
    internal sealed class TestUser(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        internal const string SchemeName = "Test";

        internal const string Header = "Test-User";

        /// <summary>An operator with the claim that grants access.</summary>
        internal const string Operator = "operator";

        /// <summary>A user of the identity provider without that claim.</summary>
        internal const string WithoutRole = "without-role";

        internal const string OperatorName = "Olivia Operator";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var user = Request.Headers[Header].ToString();
            if (string.IsNullOrEmpty(user))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            List<Claim> claims = [new("sub", user), new("name", OperatorName)];
            if (user == Operator)
            {
                claims.Add(new Claim("roles", "homeport-admin"));
            }

            var identity = new ClaimsIdentity(claims, SchemeName, "name", "roles");
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName)));
        }
    }
}
