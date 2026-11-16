// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Homeport.Shared.Client.Platform.ControlPlane.V1;

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>
/// What the operator reads of the audit logs: optionally a period, one
/// operator and one target. Every service's audit log takes the same filters.
/// </summary>
/// <param name="From">Only entries at or after this moment, or from the first.</param>
/// <param name="To">Only entries before this moment, or up to the last.</param>
/// <param name="Actor">Only entries of the operator with this subject, or of every operator.</param>
/// <param name="Target">Only entries about this target, or about every target.</param>
public sealed record AuditFilter(DateTimeOffset? From, DateTimeOffset? To, string? Actor, string? Target)
{
    // ISO 8601 in UTC with Z.
    private const string MomentFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    /// <summary>
    /// The query string of an audit log's path, with a leading <c>?</c>, for
    /// the page that starts after <paramref name="before"/>, or the first.
    /// </summary>
    /// <remarks>
    /// The control plane's parameter names stand for every service's: the
    /// audit logs share them, as the admin API's documentation says.
    /// </remarks>
    internal string QueryString(string? before)
    {
        var parameters = new List<KeyValuePair<string, string?>>();
        if (From is { } from)
        {
            parameters.Add(new(AuditApi.FromParameter, from.UtcDateTime.ToString(MomentFormat, CultureInfo.InvariantCulture)));
        }

        if (To is { } to)
        {
            parameters.Add(new(AuditApi.ToParameter, to.UtcDateTime.ToString(MomentFormat, CultureInfo.InvariantCulture)));
        }

        if (!string.IsNullOrEmpty(Actor))
        {
            parameters.Add(new(AuditApi.ActorParameter, Actor));
        }

        if (!string.IsNullOrEmpty(Target))
        {
            parameters.Add(new(AuditApi.TargetParameter, Target));
        }

        if (before is not null)
        {
            parameters.Add(new(AuditApi.BeforeParameter, before));
        }

        return Microsoft.AspNetCore.Http.QueryString.Create(parameters).ToUriComponent();
    }
}
