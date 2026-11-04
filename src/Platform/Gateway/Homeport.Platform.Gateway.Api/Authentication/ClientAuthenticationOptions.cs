// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Api.Authentication;

/// <summary>The operator's identity provider, whose client-credentials tokens the gateway accepts (ADR-0006).</summary>
public sealed class ClientAuthenticationOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "ClientAuthentication";

    /// <summary>The claim most providers carry the client id in.</summary>
    public const string DefaultClientIdClaim = "azp";

    /// <summary>The claim most providers carry the granted scopes in, space-delimited.</summary>
    public const string DefaultScopeClaim = "scope";

    /// <summary>
    /// The issuer of the tokens. Its OpenID Connect metadata, at
    /// <c>.well-known/openid-configuration</c> below it, names the signing keys.
    /// </summary>
    public Uri? Authority { get; set; }

    /// <summary>The audience a token must carry to be accepted.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The claim that carries the client id, as the control plane knows it.
    /// Providers differ: <c>azp</c>, <c>client_id</c>, <c>appid</c>.
    /// </summary>
    public string ClientIdClaim { get; set; } = DefaultClientIdClaim;

    /// <summary>
    /// The claim that carries the scopes granted to the client, space-delimited
    /// or one claim per scope. Providers differ: <c>scope</c>, <c>scp</c>.
    /// </summary>
    public string ScopeClaim { get; set; } = DefaultScopeClaim;

    /// <summary>
    /// Whether the metadata must be fetched over HTTPS. Only a development
    /// identity provider without TLS turns it off.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
