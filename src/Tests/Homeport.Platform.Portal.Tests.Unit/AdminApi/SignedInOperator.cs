// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.Portal.Tests.Unit.AdminApi;

/// <summary>
/// The request that opened a circuit, with the tokens the sign-in stored in
/// the portal's cookie, as <see cref="IHttpContextAccessor"/> hands it out.
/// </summary>
internal sealed class SignedInOperator : IHttpContextAccessor, IAuthenticationService
{
    private readonly AuthenticationProperties properties = new();

    internal SignedInOperator(string accessToken, string? refreshToken, DateTimeOffset expiresAt)
    {
        var tokens = new List<AuthenticationToken>
        {
            new() { Name = "access_token", Value = accessToken },
            new() { Name = "expires_at", Value = expiresAt.ToString("o", CultureInfo.InvariantCulture) },
        };
        if (refreshToken is not null)
        {
            tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });
        }

        properties.StoreTokens(tokens);
        HttpContext = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddSingleton<IAuthenticationService>(this).BuildServiceProvider(),
        };
    }

    public HttpContext? HttpContext { get; set; }

    /// <summary>How often the cookie was read.</summary>
    internal int Reads { get; private set; }

    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
    {
        Reads++;
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity("test")), properties, CookieAuthenticationDefaults.AuthenticationScheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => throw new NotSupportedException();

    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => throw new NotSupportedException();

    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) => throw new NotSupportedException();

    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => throw new NotSupportedException();
}
