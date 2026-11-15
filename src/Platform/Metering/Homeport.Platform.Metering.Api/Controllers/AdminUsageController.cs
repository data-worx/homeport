// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.Metering.Api.Authentication;
using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Domain.Usage;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Internal.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Homeport.Platform.Metering.Api.Controllers;

/// <summary>
/// The operator reads the usage of every tenant. Reached through the
/// gateway's admin endpoint at <c>/platform/v1/admin/usage</c>, with a caller
/// token of scope <c>admin</c> (ADR-0020). Every read is audited.
/// </summary>
[ApiController]
[Authorize(Policy = PlatformPolicies.Admin)]
public sealed class AdminUsageController(UsageAdministration administration, ILogger<AdminUsageController> logger) : ControllerBase
{
    /// <summary>
    /// Returns the usage of every tenant, or of one, per hour or day, as JSON
    /// or as CSV, and records the read in the audit log.
    /// </summary>
    /// <remarks>
    /// A bucket is in the result when it starts in [<c>from</c>, <c>to</c>).
    /// Only buckets with usage appear. <c>Accept: text/csv</c> returns CSV
    /// with a header row; anything else returns JSON.
    /// </remarks>
    /// <param name="from">The first moment of the period, ISO 8601.</param>
    /// <param name="to">The first moment after the period, ISO 8601.</param>
    /// <param name="granularity"><c>hour</c> or <c>day</c>.</param>
    /// <param name="tenant">Only this tenant's usage, by its id.</param>
    /// <param name="module">Only this module's usage.</param>
    /// <param name="meter">Only this meter's usage.</param>
    /// <param name="cancellationToken">Aborts reading the usage.</param>
    /// <response code="200">The usage, as JSON or as CSV.</response>
    /// <response code="400">A parameter is missing or invalid; <c>errors</c> says which.</response>
    /// <response code="401">No valid gateway token for metering.</response>
    /// <response code="403">The token is not the operator's: it lacks the scope <c>admin</c> or names a tenant.</response>
    [HttpGet(AdminUsageApi.Path)]
    [Produces(MediaTypeNames.Application.Json, UsageApi.CsvMediaType)]
    [ProducesResponseType<AdminUsageResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json, UsageApi.CsvMediaType)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<AdminUsageResponse>> Get(
        [FromQuery(Name = UsageApi.FromParameter), BindRequired] DateTimeOffset from,
        [FromQuery(Name = UsageApi.ToParameter), BindRequired] DateTimeOffset to,
        [FromQuery(Name = UsageApi.GranularityParameter), BindRequired] string granularity,
        [FromQuery(Name = AdminUsageApi.TenantParameter)] Guid? tenant,
        [FromQuery(Name = UsageApi.ModuleParameter)] string? module,
        [FromQuery(Name = UsageApi.MeterParameter)] string? meter,
        CancellationToken cancellationToken)
    {
        var query = UsageQuery.Create(from, to, granularity, module, meter);
        if (!query.IsSuccess)
        {
            logger.LogDebug("Admin usage read refused: {ErrorCode}", query.Error.Code);

            var fields = new ModelStateDictionary();
            fields.AddModelError(FieldOf(query.Error.Code), query.Error.Message);
            return ValidationProblem(fields);
        }

        var totals = await administration.ReadAsync(tenant, query.Value, OperatorClaims.OperatorOf(User), cancellationToken);
        return ToResponse(query.Value, totals);
    }

    /// <summary>The query parameter an error code is about.</summary>
    private static string FieldOf(string errorCode) => errorCode switch
    {
        UsageQuery.UnknownGranularityCode => UsageApi.GranularityParameter,
        UsageQuery.EmptyPeriodCode => UsageApi.ToParameter,
        _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "Every error of a usage query names its field."),
    };

    /// <summary>The wire type of the totals the query read.</summary>
    private static AdminUsageResponse ToResponse(UsageQuery query, IReadOnlyList<TenantUsageTotal> totals)
    {
        var granularity = query.Granularity switch
        {
            UsageGranularity.Hour => UsageApi.Hour,
            UsageGranularity.Day => UsageApi.Day,
            _ => throw new ArgumentOutOfRangeException(nameof(query), query.Granularity, "Every granularity has a name."),
        };

        return new AdminUsageResponse(
            query.From.UtcDateTime,
            query.To.UtcDateTime,
            granularity,
            totals.Select(total => new AdminUsageEntryResponse(total.Period.UtcDateTime, total.TenantId, total.ModuleId, total.Meter, total.Quantity)).ToList());
    }
}
