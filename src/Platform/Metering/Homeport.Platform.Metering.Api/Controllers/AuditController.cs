// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Domain;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Internal.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Homeport.Platform.Metering.Api.Controllers;

/// <summary>
/// The operator reads metering's audit log. Reached through the
/// gateway's admin endpoint at <c>/platform/v1/admin/metering/audit</c>,
/// with a caller token of scope <c>admin</c> (ADR-0020). The entries are
/// platform data, so reading them is not audited.
/// </summary>
[ApiController]
[Authorize(Policy = PlatformPolicies.Admin)]
public sealed class AuditController(IAuditReader reader, ILogger<AuditController> logger) : ControllerBase
{
    /// <summary>Returns one page of the audit log, newest first.</summary>
    /// <param name="from">Only entries at or after this moment, ISO 8601.</param>
    /// <param name="to">Only entries before this moment, ISO 8601.</param>
    /// <param name="actor">Only entries of the operator with this subject.</param>
    /// <param name="target">Only entries about this target, such as <c>tenants/{id}</c>.</param>
    /// <param name="before">The <c>next</c> of the page before; the newest entries without it.</param>
    /// <param name="limit">The most entries the page holds, 1 to 200; 50 without it.</param>
    /// <param name="cancellationToken">Aborts reading the entries.</param>
    /// <response code="200">The page.</response>
    /// <response code="400">A parameter is invalid; <c>errors</c> says which.</response>
    /// <response code="401">No valid gateway token for metering.</response>
    /// <response code="403">The token is not the operator's: it lacks the scope <c>admin</c> or names a tenant.</response>
    [HttpGet(AuditApi.Path)]
    [ProducesResponseType<AuditEntriesResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<AuditEntriesResponse>> Get(
        [FromQuery(Name = AuditApi.FromParameter)] DateTimeOffset? from,
        [FromQuery(Name = AuditApi.ToParameter)] DateTimeOffset? to,
        [FromQuery(Name = AuditApi.ActorParameter)] string? actor,
        [FromQuery(Name = AuditApi.TargetParameter)] string? target,
        [FromQuery(Name = AuditApi.BeforeParameter)] string? before,
        [FromQuery(Name = AuditApi.LimitParameter)] int? limit,
        CancellationToken cancellationToken)
    {
        var query = AuditQuery.Create(from, to, actor, target, before, limit);
        if (!query.IsSuccess)
        {
            logger.LogDebug("Audit read refused: {ErrorCode}", query.Error.Code);

            var fields = new ModelStateDictionary();
            fields.AddModelError(FieldOf(query.Error.Code), query.Error.Message);
            return ValidationProblem(fields);
        }

        var page = await reader.ReadAsync(query.Value, cancellationToken);
        return new AuditEntriesResponse(page.Entries.Select(ToResponse).ToList(), page.Next?.ToString());
    }

    /// <summary>The query parameter an error code is about.</summary>
    private static string FieldOf(string errorCode) => errorCode switch
    {
        AuditQuery.EmptyPeriodCode => AuditApi.ToParameter,
        AuditQuery.ActorTooLongCode => AuditApi.ActorParameter,
        AuditQuery.TargetTooLongCode => AuditApi.TargetParameter,
        AuditQuery.InvalidCursorCode => AuditApi.BeforeParameter,
        AuditQuery.LimitOutOfRangeCode => AuditApi.LimitParameter,
        _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "Every error of an audit query names its field."),
    };

    /// <summary>The wire type of an entry.</summary>
    private static AuditEntryResponse ToResponse(AuditEntry entry)
    {
        return new AuditEntryResponse(
            entry.Id,
            entry.OccurredAt.UtcDateTime,
            entry.OperatorSubject,
            entry.OperatorName,
            entry.Action,
            entry.Target,
            entry.Summary);
    }
}
