// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Homeport.Shared.Client.Platform.Metering.V1;

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>
/// What the operator reads of the usage of every tenant: a period, per hour or
/// day, and optionally one tenant, module or meter.
/// </summary>
/// <param name="From">The first moment of the period.</param>
/// <param name="To">The first moment after the period.</param>
/// <param name="Granularity"><see cref="UsageApi.Hour"/> or <see cref="UsageApi.Day"/>.</param>
/// <param name="Tenant">Only this tenant's usage, or every tenant's.</param>
/// <param name="Module">Only this module's usage, or every module's.</param>
/// <param name="Meter">Only this meter's usage, or every meter's.</param>
public sealed record UsageFilter(DateTimeOffset From, DateTimeOffset To, string Granularity, Guid? Tenant, string? Module, string? Meter)
{
    // ISO 8601 in UTC with Z, as metering writes the period back.
    private const string MomentFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    /// <summary>The query string of the admin API's usage path, with a leading <c>?</c>.</summary>
    internal string QueryString()
    {
        var parameters = new List<KeyValuePair<string, string?>>
        {
            new(UsageApi.FromParameter, From.UtcDateTime.ToString(MomentFormat, CultureInfo.InvariantCulture)),
            new(UsageApi.ToParameter, To.UtcDateTime.ToString(MomentFormat, CultureInfo.InvariantCulture)),
            new(UsageApi.GranularityParameter, Granularity),
        };

        if (Tenant is { } tenant)
        {
            parameters.Add(new(AdminUsageApi.TenantParameter, tenant.ToString()));
        }

        if (Module is not null)
        {
            parameters.Add(new(UsageApi.ModuleParameter, Module));
        }

        if (Meter is not null)
        {
            parameters.Add(new(UsageApi.MeterParameter, Meter));
        }

        return Microsoft.AspNetCore.Http.QueryString.Create(parameters).ToUriComponent();
    }
}
