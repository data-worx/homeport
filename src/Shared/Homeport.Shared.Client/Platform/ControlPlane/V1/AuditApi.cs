// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.ControlPlane.V1;

/// <summary>
/// The audit log of the control plane in the admin API (ADR-0020):
/// <c>GET /platform/v1/admin/control-plane/audit</c> on the gateway's admin
/// endpoint, for the operator only. It returns the changes the operator made to
/// tenants, plans and clients as
/// <see cref="AuditEntriesResponse"/>, newest first, one page at a time.
/// </summary>
public static class AuditApi
{
    /// <summary>The path below <c>/platform</c>, the path the control plane serves it at.</summary>
    public const string Path = "/v1/admin/control-plane/audit";

    /// <summary>The optional query parameter with the first moment of the period, ISO 8601.</summary>
    public const string FromParameter = "from";

    /// <summary>The optional query parameter with the first moment after the period, ISO 8601.</summary>
    public const string ToParameter = "to";

    /// <summary>The optional query parameter that limits the entries to one operator, by their subject.</summary>
    public const string ActorParameter = "actor";

    /// <summary>The optional query parameter that limits the entries to one target, such as <c>tenants/{id}</c>.</summary>
    public const string TargetParameter = "target";

    /// <summary>
    /// The optional query parameter with the <see cref="AuditEntriesResponse.Next"/>
    /// of the page before: the entries older than that page's last.
    /// </summary>
    public const string BeforeParameter = "before";

    /// <summary>The optional query parameter with the most entries a page holds.</summary>
    public const string LimitParameter = "limit";

    /// <summary>The entries a page holds without <see cref="LimitParameter"/>.</summary>
    public const int DefaultLimit = 50;

    /// <summary>The most entries a page holds.</summary>
    public const int MaxLimit = 200;
}
