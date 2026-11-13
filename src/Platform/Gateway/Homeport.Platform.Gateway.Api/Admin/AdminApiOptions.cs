// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Api.Admin;

/// <summary>
/// The admin API (ADR-0020): the port it answers on, and the operator's
/// identity provider whose tokens it accepts. Without a port the admin API is
/// off, and every path under it answers like an unknown path.
/// </summary>
public sealed class AdminApiOptions
{
    /// <summary>The configuration section these options are bound from.</summary>
    public const string SectionName = "AdminApi";

    /// <summary>The claim type that grants admin access unless configured otherwise.</summary>
    public const string DefaultClaimType = "roles";

    /// <summary>The claim value that grants admin access unless configured otherwise.</summary>
    public const string DefaultClaimValue = "homeport-admin";

    /// <summary>The claim the operator's display name is taken from unless configured otherwise.</summary>
    public const string DefaultNameClaim = "name";

    /// <summary>
    /// The port of the admin endpoint, one of the ports the gateway listens
    /// on. Requests on it reach the admin API and nothing else; requests on
    /// every other port never reach the admin API. Unset, the admin API is off.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// The issuer of the operator's tokens. Its OpenID Connect metadata, at
    /// <c>.well-known/openid-configuration</c> below it, names the signing keys.
    /// </summary>
    public Uri? Authority { get; set; }

    /// <summary>The audience an operator's token must carry to be accepted.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>The type of the claim that grants admin access, such as <c>roles</c>.</summary>
    public string ClaimType { get; set; } = DefaultClaimType;

    /// <summary>The value the claim must carry, among others if it holds several.</summary>
    public string ClaimValue { get; set; } = DefaultClaimValue;

    /// <summary>
    /// The claim that carries the operator's display name, which the caller
    /// token passes on for the audit. Without it, the subject stands in.
    /// </summary>
    public string NameClaim { get; set; } = DefaultNameClaim;

    /// <summary>
    /// Whether the metadata must be fetched over HTTPS. Only a development
    /// identity provider without TLS turns it off.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;
}
