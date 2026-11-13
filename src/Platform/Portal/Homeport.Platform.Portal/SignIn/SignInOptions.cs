// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Portal.SignIn;

/// <summary>
/// How the operator signs in (ADR-0020): the identity provider, the portal's
/// client there, and the claim that grants access.
/// </summary>
public sealed class SignInOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "SignIn";

    /// <summary>The claim type that grants access unless configured otherwise.</summary>
    public const string DefaultClaimType = "roles";

    /// <summary>The claim value that grants access unless configured otherwise.</summary>
    public const string DefaultClaimValue = "homeport-admin";

    /// <summary>The claim the operator's display name is taken from unless configured otherwise.</summary>
    public const string DefaultNameClaim = "name";

    /// <summary>
    /// The issuer of the operator's tokens. Its OpenID Connect metadata, at
    /// <c>.well-known/openid-configuration</c> below it, names the endpoints.
    /// </summary>
    public Uri? Authority { get; set; }

    /// <summary>The portal's client id at the identity provider.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>The secret of the portal's confidential client.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>The type of the claim that grants access, such as <c>roles</c>.</summary>
    public string ClaimType { get; set; } = DefaultClaimType;

    /// <summary>The value the claim must carry, among others if it holds several.</summary>
    public string ClaimValue { get; set; } = DefaultClaimValue;

    /// <summary>The claim that carries the operator's display name.</summary>
    public string NameClaim { get; set; } = DefaultNameClaim;

    /// <summary>
    /// Whether the metadata must be fetched over HTTPS. Only a development
    /// identity provider without TLS turns it off.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
