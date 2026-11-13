// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.ControlPlane.Api.Authentication;
using Homeport.Platform.ControlPlane.Application.Plans;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Homeport.Platform.ControlPlane.Api.Controllers;

/// <summary>
/// The operator lists, creates, changes and deletes plans. Reached through
/// the gateway's admin endpoint at <c>/platform/v1/admin/plans</c>, with a
/// caller token of scope <c>admin</c> (ADR-0020).
/// </summary>
[ApiController]
[Authorize(Policy = AdminRequirement.Policy)]
public sealed class PlansController(PlanAdministration plans, ILogger<PlansController> logger) : ControllerBase
{
    /// <summary>Returns every plan, ordered by name.</summary>
    /// <param name="cancellationToken">Aborts reading the plans.</param>
    /// <response code="200">The plans.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's: it lacks the scope <c>admin</c> or names a tenant.</response>
    [HttpGet(PlansApi.Path)]
    [ProducesResponseType<PlansResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<PlansResponse>> List(CancellationToken cancellationToken)
    {
        var all = await plans.ListAsync(cancellationToken);
        return new PlansResponse(all.Select(ToResponse).ToList());
    }

    /// <summary>Returns one plan.</summary>
    /// <param name="planId">The plan's id.</param>
    /// <param name="cancellationToken">Aborts reading the plan.</param>
    /// <response code="200">The plan.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    /// <response code="404">There is no such plan.</response>
    [HttpGet(PlansApi.PlanPath)]
    [ProducesResponseType<PlanResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<PlanResponse>> Get(Guid planId, CancellationToken cancellationToken)
    {
        return ToActionResult(await plans.GetAsync(planId, cancellationToken));
    }

    /// <summary>Creates a plan and records the change in the audit log.</summary>
    /// <param name="request">The new plan.</param>
    /// <param name="cancellationToken">Aborts the change.</param>
    /// <response code="201">The plan was created.</response>
    /// <response code="400">The name is empty, too long or taken, or a limit is not positive; <c>errors</c> names the field.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    [HttpPost(PlansApi.Path)]
    [ProducesResponseType<PlanResponse>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<PlanResponse>> Create(CreatePlanRequest request, CancellationToken cancellationToken)
    {
        var created = await plans.CreateAsync(request.Name, request.RequestsPerSecond, request.Burst, AdminHandler.OperatorOf(User), cancellationToken);
        if (!created.IsSuccess)
        {
            return ToActionResult(created);
        }

        var plan = created.Value;
        logger.LogInformation("Plan {PlanId} created", plan.Id);
        return CreatedAtAction(nameof(Get), new { planId = plan.Id }, ToResponse(plan));
    }

    /// <summary>Changes a plan's name and limits and records the change in the audit log.</summary>
    /// <param name="planId">The plan's id.</param>
    /// <param name="request">The plan's new name and limits.</param>
    /// <param name="cancellationToken">Aborts the change.</param>
    /// <response code="200">The plan, changed; unchanged and not audited when nothing differed.</response>
    /// <response code="400">The name is empty, too long or taken, or a limit is not positive; <c>errors</c> names the field.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    /// <response code="404">There is no such plan.</response>
    [HttpPut(PlansApi.PlanPath)]
    [ProducesResponseType<PlanResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<PlanResponse>> Change(Guid planId, ChangePlanRequest request, CancellationToken cancellationToken)
    {
        var changed = await plans.ChangeAsync(planId, request.Name, request.RequestsPerSecond, request.Burst, AdminHandler.OperatorOf(User), cancellationToken);
        if (changed.IsSuccess)
        {
            logger.LogInformation("Plan {PlanId} changed", planId);
        }

        return ToActionResult(changed);
    }

    /// <summary>Deletes a plan no client uses and records the change in the audit log.</summary>
    /// <param name="planId">The plan's id.</param>
    /// <param name="cancellationToken">Aborts the change.</param>
    /// <response code="204">The plan was deleted.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    /// <response code="404">There is no such plan.</response>
    /// <response code="409">A client uses the plan: <c>…/problems/plan-in-use</c>.</response>
    [HttpDelete(PlansApi.PlanPath)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Delete(Guid planId, CancellationToken cancellationToken)
    {
        var deleted = await plans.DeleteAsync(planId, AdminHandler.OperatorOf(User), cancellationToken);
        if (!deleted.IsSuccess)
        {
            return ToActionResult(deleted).Result!;
        }

        logger.LogInformation("Plan {PlanId} deleted", planId);
        return NoContent();
    }

    private ActionResult<PlanResponse> ToActionResult(Result<PlanOverview> result)
    {
        if (result.IsSuccess)
        {
            return ToResponse(result.Value);
        }

        logger.LogDebug("Plan request refused: {ErrorCode}", result.Error.Code);
        switch (result.Error.Code)
        {
            case PlanAdministration.NotFoundCode:
                return Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound);
            case Plan.InUseCode:
                return Problem(detail: result.Error.Message, statusCode: StatusCodes.Status409Conflict);
        }

        var fields = new ModelStateDictionary();
        fields.AddModelError(FieldOf(result.Error.Code), result.Error.Message);
        return ValidationProblem(fields);
    }

    private static string FieldOf(string errorCode) => errorCode switch
    {
        Plan.NameRequiredCode or Plan.NameTooLongCode or Plan.NameTakenCode => PlansApi.NameField,
        Plan.RequestsPerSecondNotPositiveCode => PlansApi.RequestsPerSecondField,
        Plan.BurstNotPositiveCode => PlansApi.BurstField,
        _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "Every error of a plan change names its field."),
    };

    private static PlanResponse ToResponse(PlanOverview plan)
    {
        return new PlanResponse(plan.Id, plan.Name, plan.RequestsPerSecond, plan.Burst, plan.ClientCount);
    }
}
