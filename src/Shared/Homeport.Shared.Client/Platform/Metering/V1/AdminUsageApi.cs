// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Metering.V1;

/// <summary>
/// The usage of every tenant in the admin API (ADR-0020):
/// <c>GET /platform/v1/admin/usage</c> on the gateway's admin endpoint, for
/// the operator only. It takes the query parameters of <see cref="UsageApi"/>
/// plus <see cref="TenantParameter"/>, and returns
/// <see cref="AdminUsageResponse"/> in JSON, or CSV with
/// <see cref="UsageApi.CsvMediaType"/> in <c>Accept</c>.
/// </summary>
public static class AdminUsageApi
{
    /// <summary>The path below <c>/platform</c>, the path metering serves it at.</summary>
    public const string Path = "/v1/admin/usage";

    /// <summary>The optional query parameter that limits the usage to one tenant, by its id.</summary>
    public const string TenantParameter = "tenant";
}
