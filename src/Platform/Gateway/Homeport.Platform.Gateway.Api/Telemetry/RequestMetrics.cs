// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.Metrics;
using Homeport.Platform.Gateway.Infrastructure.Usage;
using Homeport.Shared.ServiceDefaults;

namespace Homeport.Platform.Gateway.Api.Telemetry;

/// <summary>The gateway's request metrics, on the meter of the gateway's host.</summary>
internal sealed class RequestMetrics
{
    /// <summary>
    /// Counts requests forwarded to a module or refused on a module's route,
    /// tagged with <see cref="TelemetryAttributes.ModuleId"/> and <see cref="StatusCodeTag"/>.
    /// </summary>
    internal const string RequestsInstrument = "homeport.gateway.requests";

    /// <summary>The status code the gateway answered with, as OpenTelemetry names it.</summary>
    internal const string StatusCodeTag = "http.response.status_code";

    private readonly Counter<long> requests;

    public RequestMetrics(IMeterFactory meters)
    {
        requests = meters.Create(UsageMetrics.MeterName)
            .CreateCounter<long>(RequestsInstrument, description: "Requests on a module's route, by module and status code.");
    }

    /// <summary>Counts one request to <paramref name="moduleId"/>, answered with <paramref name="statusCode"/>.</summary>
    public void Answered(string moduleId, int statusCode)
    {
        requests.Add(
            1,
            new KeyValuePair<string, object?>(TelemetryAttributes.ModuleId, moduleId),
            new KeyValuePair<string, object?>(StatusCodeTag, statusCode));
    }
}
