// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Homeport.Platform.Portal.SignIn;

/// <summary>Wires the operator's sign-in (ADR-0020).</summary>
internal static class SignInServiceCollectionExtensions
{
    /// <summary>
    /// A cookie for the signed-in operator and OIDC, authorization code with
    /// PKCE, at the identity provider of <see cref="SignInOptions"/>. Every
    /// page requires the claim that grants access unless it allows anonymous
    /// callers. An anonymous request lands on the sign-in page, not at the
    /// identity provider; only its button starts the sign-in there.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <returns>The same services, for chaining.</returns>
    internal static IServiceCollection AddOperatorSignIn(this IServiceCollection services)
    {
        services.AddOptions<SignInOptions>()
            .BindConfiguration(SignInOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SignInOptions>, SignInOptionsValidator>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = PortalPaths.SignIn;
                options.AccessDeniedPath = PortalPaths.AccessDenied;
                options.ReturnUrlParameter = PortalPaths.ReturnUrlParameter;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
            })
            .AddOpenIdConnect(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.UsePkce = true;

                // The code comes back in the query of a top-level navigation,
                // so the correlation and nonce cookies can be Lax as well; a
                // form post from the identity provider would need None.
                options.ResponseMode = OpenIdConnectResponseMode.Query;
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.NonceCookie.SameSite = SameSiteMode.Lax;
                options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;

                options.CallbackPath = PortalPaths.SignInCallback;
                options.SignedOutCallbackPath = PortalPaths.SignOutCallback;
                options.SignedOutRedirectUri = PortalPaths.SignIn;

                // The access token goes to the admin API; claims keep the
                // names the identity provider gave them.
                options.SaveTokens = true;
                options.MapInboundClaims = false;
                options.Scope.Clear();
                options.Scope.Add(OpenIdConnectScope.OpenIdProfile);

                options.EventsType = typeof(OperatorSignInEvents);
            });
        services.AddOptions<OpenIdConnectOptions>(OpenIdConnectDefaults.AuthenticationScheme)
            .Configure<IOptions<SignInOptions>>((oidc, signIn) =>
            {
                oidc.Authority = signIn.Value.Authority?.ToString();
                oidc.ClientId = signIn.Value.ClientId;
                oidc.ClientSecret = signIn.Value.ClientSecret;
                oidc.RequireHttpsMetadata = signIn.Value.RequireHttpsMetadata;
                oidc.TokenValidationParameters.NameClaimType = signIn.Value.NameClaim;
                oidc.TokenValidationParameters.RoleClaimType = signIn.Value.ClaimType;
            });

        services.AddSingleton<OperatorSignInEvents>();
        services.AddSingleton<IAuthorizationHandler, OperatorRequirementHandler>();
        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new OperatorRequirement())
                .Build());
        services.AddCascadingAuthenticationState();

        return services;
    }
}
