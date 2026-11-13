// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>
/// Gets the operator a new access token at the identity provider's token
/// endpoint with the refresh token, as the portal's client (RFC 6749, 6).
/// </summary>
internal sealed class OperatorTokenRefresher(
    IOptionsMonitor<OpenIdConnectOptions> oidc,
    TimeProvider clock,
    ILogger<OperatorTokenRefresher> logger)
{
    private const string GrantType = "grant_type";
    private const string RefreshTokenGrant = "refresh_token";
    private const string RefreshToken = "refresh_token";
    private const string ClientId = "client_id";
    private const string ClientSecret = "client_secret";

    /// <summary>
    /// The new tokens, or <see langword="null"/> when there is no refresh token
    /// or the identity provider refuses it, as it does once the operator's
    /// session there has ended.
    /// </summary>
    internal async Task<OperatorTokenSet?> RefreshAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(refreshToken))
        {
            return null;
        }

        var options = oidc.Get(OpenIdConnectDefaults.AuthenticationScheme);
        var configuration = options.Configuration
            ?? await options.ConfigurationManager!.GetConfigurationAsync(cancellationToken);
        using var request = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [GrantType] = RefreshTokenGrant,
            [RefreshToken] = refreshToken,
            [ClientId] = options.ClientId!,
            [ClientSecret] = options.ClientSecret!,
        });

        using var response = await options.Backchannel.PostAsync(configuration.TokenEndpoint, request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning("The identity provider refused to refresh the operator's token with {StatusCode}; the operator signs in again", (int)response.StatusCode);
            return null;
        }

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);
        if (tokens?.AccessToken is null)
        {
            logger.LogWarning("The identity provider answered a refresh without an access token; the operator signs in again");
            return null;
        }

        logger.LogDebug("Refreshed the operator's access token");
        return new OperatorTokenSet(
            tokens.AccessToken,
            tokens.RefreshToken ?? refreshToken,
            clock.GetUtcNow().AddSeconds(tokens.ExpiresIn));
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string? AccessToken,
        [property: JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
