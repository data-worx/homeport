// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.Metering.Api.Authentication;
using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Domain.Usage;
using Homeport.Shared.Client.Platform.Metering.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Homeport.Platform.Metering.Api.Controllers;

/// <summary>
/// A tenant's own usage, for showback and chargeback. Reached through the
/// gateway at <c>/platform/v1/usage</c>, with a caller token that names the
/// tenant (ADR-0019).
/// </summary>
[ApiController]
[Authorize(Policy = UsageReaderRequirement.Policy)]
public sealed class UsageController(UsageExport export, ILogger<UsageController> logger) : ControllerBase
{
    /// <summary>Returns the caller's usage per hour or day, as JSON or as CSV.</summary>
    /// <remarks>
    /// A bucket is in the export when it starts in [<c>from</c>, <c>to</c>).
    /// Only buckets with usage appear. <c>Accept: text/csv</c> returns CSV
    /// with a header row; anything else returns JSON.
    /// </remarks>
    /// <param name="from">The first moment of the period, ISO 8601.</param>
    /// <param name="to">The first moment after the period, ISO 8601.</param>
    /// <param name="granularity"><c>hour</c> or <c>day</c>.</param>
    /// <param name="module">Only this module's usage.</param>
    /// <param name="meter">Only this meter's usage.</param>
    /// <param name="cancellationToken">Aborts reading the usage.</param>
    /// <response code="200">The usage, as JSON or as CSV.</response>
    /// <response code="400">A parameter is missing or invalid; <c>errors</c> says which.</response>
    /// <response code="401">No valid gateway token for metering.</response>
    /// <response code="403">The token names no tenant or lacks the scope <c>usage.read</c>.</response>
    [HttpGet(UsageApi.Path)]
    [Produces(MediaTypeNames.Application.Json, UsageApi.CsvMediaType)]
    [ProducesResponseType<UsageResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json, UsageApi.CsvMediaType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<UsageResponse>> Get(
        [FromQuery(Name = UsageApi.FromParameter), BindRequired] DateTimeOffset from,
        [FromQuery(Name = UsageApi.ToParameter), BindRequired] DateTimeOffset to,
        [FromQuery(Name = UsageApi.GranularityParameter), BindRequired] string granularity,
        [FromQuery(Name = UsageApi.ModuleParameter)] string? module,
        [FromQuery(Name = UsageApi.MeterParameter)] string? meter,
        CancellationToken cancellationToken)
    {
        var query = UsageQuery.Create(from, to, granularity, module, meter);
        if (!query.IsSuccess)
        {
            logger.LogDebug("Usage export refused: {ErrorCode}", query.Error.Code);

            var fields = new ModelStateDictionary();
            fields.AddModelError(FieldOf(query.Error.Code), query.Error.Message);
            return ValidationProblem(fields);
        }

        // The policy has made sure the token names one.
        var tenantId = UsageReaderHandler.TenantIdOf(User)
            ?? throw new InvalidOperationException("A usage export reached the controller without a tenant.");

        var totals = await export.ReadAsync(tenantId, query.Value, cancellationToken);
        return ToResponse(query.Value, totals);
    }

    private static string FieldOf(string errorCode) => errorCode switch
    {
        UsageQuery.UnknownGranularityCode => UsageApi.GranularityParameter,
        UsageQuery.EmptyPeriodCode => UsageApi.ToParameter,
        _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "Every error of a usage query names its field."),
    };

    private static UsageResponse ToResponse(UsageQuery query, IReadOnlyList<UsageTotal> totals)
    {
        var granularity = query.Granularity switch
        {
            UsageGranularity.Hour => UsageApi.Hour,
            UsageGranularity.Day => UsageApi.Day,
            _ => throw new ArgumentOutOfRangeException(nameof(query), query.Granularity, "Every granularity has a name."),
        };

        return new UsageResponse(
            query.From.UtcDateTime,
            query.To.UtcDateTime,
            granularity,
            totals.Select(total => new UsageEntryResponse(total.Period.UtcDateTime, total.ModuleId, total.Meter, total.Quantity)).ToList());
    }
}
