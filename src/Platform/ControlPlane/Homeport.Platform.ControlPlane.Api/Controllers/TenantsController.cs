// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.ControlPlane.Api.Authentication;
using Homeport.Platform.ControlPlane.Application.Tenants;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Homeport.Platform.ControlPlane.Api.Controllers;

/// <summary>
/// The operator lists, creates and renames tenants. Reached through the
/// gateway's admin endpoint at <c>/platform/v1/admin/tenants</c>, with a
/// caller token of scope <c>admin</c> (ADR-0020).
/// </summary>
[ApiController]
[Authorize(Policy = AdminRequirement.Policy)]
public sealed class TenantsController(TenantAdministration tenants, ILogger<TenantsController> logger) : ControllerBase
{
    /// <summary>Returns every tenant, ordered by name.</summary>
    /// <param name="cancellationToken">Aborts reading the tenants.</param>
    /// <response code="200">The tenants.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's: it lacks the scope <c>admin</c> or names a tenant.</response>
    [HttpGet(TenantsApi.Path)]
    [ProducesResponseType<TenantsResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<TenantsResponse>> List(CancellationToken cancellationToken)
    {
        var all = await tenants.ListAsync(cancellationToken);
        return new TenantsResponse(all.Select(ToResponse).ToList());
    }

    /// <summary>Returns one tenant.</summary>
    /// <param name="tenantId">The tenant's id.</param>
    /// <param name="cancellationToken">Aborts reading the tenant.</param>
    /// <response code="200">The tenant.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    /// <response code="404">There is no such tenant.</response>
    [HttpGet(TenantsApi.TenantPath)]
    [ProducesResponseType<TenantResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<TenantResponse>> Get(Guid tenantId, CancellationToken cancellationToken)
    {
        return ToActionResult(await tenants.GetAsync(tenantId, cancellationToken));
    }

    /// <summary>Creates a tenant and records the change in the audit log.</summary>
    /// <param name="request">The new tenant.</param>
    /// <param name="cancellationToken">Aborts the change.</param>
    /// <response code="201">The tenant was created.</response>
    /// <response code="400">The name is empty, too long or taken; <c>errors.name</c> says why.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    [HttpPost(TenantsApi.Path)]
    [ProducesResponseType<TenantResponse>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<TenantResponse>> Create(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var created = await tenants.CreateAsync(request.Name, AdminHandler.OperatorOf(User), cancellationToken);
        if (!created.IsSuccess)
        {
            return ToActionResult(created);
        }

        var tenant = created.Value;
        logger.LogInformation("Tenant {TenantId} created", tenant.Id);
        return CreatedAtAction(nameof(Get), new { tenantId = tenant.Id }, ToResponse(tenant));
    }

    /// <summary>Renames a tenant and records the change in the audit log.</summary>
    /// <param name="tenantId">The tenant's id.</param>
    /// <param name="request">The tenant's new name.</param>
    /// <param name="cancellationToken">Aborts the change.</param>
    /// <response code="200">The tenant, renamed; unchanged and not audited when the name was its own.</response>
    /// <response code="400">The name is empty, too long or taken; <c>errors.name</c> says why.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    /// <response code="404">There is no such tenant.</response>
    [HttpPut(TenantsApi.TenantPath)]
    [ProducesResponseType<TenantResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<TenantResponse>> Rename(Guid tenantId, RenameTenantRequest request, CancellationToken cancellationToken)
    {
        var renamed = await tenants.RenameAsync(tenantId, request.Name, AdminHandler.OperatorOf(User), cancellationToken);
        if (renamed.IsSuccess)
        {
            logger.LogInformation("Tenant {TenantId} renamed", tenantId);
        }

        return ToActionResult(renamed);
    }

    private ActionResult<TenantResponse> ToActionResult(Result<TenantOverview> result)
    {
        if (result.IsSuccess)
        {
            return ToResponse(result.Value);
        }

        logger.LogDebug("Tenant request refused: {ErrorCode}", result.Error.Code);
        if (result.Error.Code == TenantAdministration.NotFoundCode)
        {
            return Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound);
        }

        var fields = new ModelStateDictionary();
        fields.AddModelError(FieldOf(result.Error.Code), result.Error.Message);
        return ValidationProblem(fields);
    }

    private static string FieldOf(string errorCode) => errorCode switch
    {
        Tenant.NameRequiredCode or Tenant.NameTooLongCode or Tenant.NameTakenCode => TenantsApi.NameField,
        _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "Every error of a tenant change names its field."),
    };

    private static TenantResponse ToResponse(TenantOverview tenant)
    {
        return new TenantResponse(tenant.Id, tenant.Name, tenant.ClientCount);
    }
}
