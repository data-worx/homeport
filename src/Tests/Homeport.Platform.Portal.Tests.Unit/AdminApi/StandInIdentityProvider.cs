// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Mime;
using System.Text;
using Homeport.Platform.Portal.AdminApi;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Homeport.Platform.Portal.Tests.Unit.AdminApi;

/// <summary>
/// The identity provider's token endpoint, which answers a refresh with
/// <see cref="NewAccessToken"/> valid for <see cref="Lifetime"/>, and the
/// portal's OIDC options pointing at it.
/// </summary>
internal sealed class StandInIdentityProvider : IOptionsMonitor<OpenIdConnectOptions>
{
    internal const string TokenEndpoint = "https://idp.example/realms/homeport/protocol/openid-connect/token";

    internal const string ClientId = "portal";

    internal const string ClientSecret = "portal-secret";

    internal const string NewAccessToken = "new-access-token";

    internal const string NewRefreshToken = "new-refresh-token";

    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly OpenIdConnectOptions options;

    internal StandInIdentityProvider()
    {
        Handler.Respond = _ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""{"access_token":"{{NewAccessToken}}","refresh_token":"{{NewRefreshToken}}","expires_in":{{(int)Lifetime.TotalSeconds}},"token_type":"Bearer"}""",
                Encoding.UTF8,
                MediaTypeNames.Application.Json),
        };
        options = new OpenIdConnectOptions
        {
            ClientId = ClientId,
            ClientSecret = ClientSecret,
            Configuration = new OpenIdConnectConfiguration { TokenEndpoint = TokenEndpoint },
            Backchannel = new HttpClient(Handler),
        };
    }

    /// <summary>The token endpoint's handler: what it answers, and what it received.</summary>
    internal StandInHandler Handler { get; } = new();

    /// <summary>The clock the refresher and the tokens run on.</summary>
    internal FakeTimeProvider Clock { get; } = new(DateTimeOffset.Parse("2026-11-13T12:00:00Z"));

    public OpenIdConnectOptions CurrentValue => options;

    internal OperatorTokenRefresher Refresher() => new(this, Clock, NullLogger<OperatorTokenRefresher>.Instance);

    public OpenIdConnectOptions Get(string? name) => options;

    public IDisposable? OnChange(Action<OpenIdConnectOptions, string?> listener) => null;
}
