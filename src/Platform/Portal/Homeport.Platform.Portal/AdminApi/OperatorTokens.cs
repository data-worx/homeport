// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>
/// The operator's access token for the admin API, for one circuit.
/// </summary>
/// <remarks>
/// The sign-in stores the tokens in the portal's cookie. A circuit reads them
/// from there once, through the request that opened it, and from then on
/// keeps them in memory: a circuit outlives an access token, and the cookie
/// cannot be rewritten over its connection. Shortly before the access token
/// expires, the circuit gets a new one with the refresh token.
/// </remarks>
internal sealed class OperatorTokens(
    IHttpContextAccessor accessor,
    OperatorTokenRefresher refresher,
    TimeProvider clock) : IDisposable
{
    /// <summary>How long before it expires an access token is renewed, so it does not expire on its way.</summary>
    internal static readonly TimeSpan RenewalMargin = TimeSpan.FromSeconds(30);

    private const string AccessTokenName = "access_token";
    private const string RefreshTokenName = "refresh_token";
    private const string ExpiresAtName = "expires_at";

    private readonly SemaphoreSlim gate = new(1, 1);
    private OperatorTokenSet? tokens;
    private bool loaded;

    /// <summary>
    /// A valid access token, or <see langword="null"/> when the operator has to
    /// sign in again.
    /// </summary>
    internal async Task<string?> AccessTokenAsync(CancellationToken cancellationToken)
    {
        await gate.WaitAsync(cancellationToken);
        try
        {
            if (!loaded)
            {
                tokens = await FromCookieAsync();
                loaded = true;
            }

            if (tokens is not null && tokens.ExpiresAt - RenewalMargin <= clock.GetUtcNow())
            {
                tokens = await refresher.RefreshAsync(tokens.RefreshToken, cancellationToken);
            }

            return tokens?.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Dispose() => gate.Dispose();

    private async Task<OperatorTokenSet?> FromCookieAsync()
    {
        if (accessor.HttpContext is not { } context)
        {
            return null;
        }

        var ticket = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = ticket.Properties;
        var accessToken = properties?.GetTokenValue(AccessTokenName);
        if (properties is null || accessToken is null)
        {
            return null;
        }

        // Without an expiry, the token is treated as expired, so it is renewed before it is used.
        var expiresAt = DateTimeOffset.TryParse(properties.GetTokenValue(ExpiresAtName), CultureInfo.InvariantCulture, DateTimeStyles.None, out var at)
            ? at
            : DateTimeOffset.MinValue;

        return new OperatorTokenSet(accessToken, properties.GetTokenValue(RefreshTokenName), expiresAt);
    }
}
