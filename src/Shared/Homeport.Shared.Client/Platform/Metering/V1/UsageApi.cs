// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Metering.V1;

/// <summary>
/// The usage export: <c>GET /platform/v1/usage</c> at the gateway returns a
/// tenant's own usage as <see cref="UsageResponse"/> in JSON, or as CSV with
/// <see cref="CsvMediaType"/> in <c>Accept</c>.
/// </summary>
public static class UsageApi
{
    /// <summary>The path below <c>/platform</c>, the path metering serves it at.</summary>
    public const string Path = "/v1/usage";

    /// <summary>The scope a client's token needs to read its tenant's usage.</summary>
    public const string ReadScope = "usage.read";

    /// <summary>The media type of the CSV export.</summary>
    public const string CsvMediaType = "text/csv";

    /// <summary>The query parameter with the first moment of the period, ISO 8601.</summary>
    public const string FromParameter = "from";

    /// <summary>The query parameter with the first moment after the period, ISO 8601.</summary>
    public const string ToParameter = "to";

    /// <summary>The query parameter with <see cref="Hour"/> or <see cref="Day"/>.</summary>
    public const string GranularityParameter = "granularity";

    /// <summary>The optional query parameter that limits the export to one module.</summary>
    public const string ModuleParameter = "module";

    /// <summary>The optional query parameter that limits the export to one meter.</summary>
    public const string MeterParameter = "meter";

    /// <summary>One row per UTC hour.</summary>
    public const string Hour = "hour";

    /// <summary>One row per UTC day.</summary>
    public const string Day = "day";
}
