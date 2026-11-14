// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.ControlPlane.Api.Authentication;
using Homeport.Platform.ControlPlane.Application.Clients;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Homeport.Platform.ControlPlane.Api.Controllers;

/// <summary>
/// The operator registers the clients of a tenant, moves them to another plan
/// and removes them. Reached through the gateway's admin endpoint at
/// <c>/platform/v1/admin/clients</c>, with a caller token of scope
/// <c>admin</c> (ADR-0020). The gateway's own lookup of a client is
/// <see cref="ClientsController"/>.
/// </summary>
[ApiController]
[Authorize(Policy = AdminRequirement.Policy)]
public sealed class AdminClientsController(ClientAdministration clients, ILogger<AdminClientsController> logger) : ControllerBase
{
    /// <summary>Returns every registered client, or every client of one tenant, ordered by client id.</summary>
    /// <param name="tenantId">Only the clients of this tenant; every client when it is absent.</param>
    /// <param name="cancellationToken">Aborts reading the clients.</param>
    /// <response code="200">The clients; none for a tenant that does not exist.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's: it lacks the scope <c>admin</c> or names a tenant.</response>
    [HttpGet(ClientsApi.Path)]
    [ProducesResponseType<ClientsResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ClientsResponse>> List([FromQuery(Name = ClientsApi.TenantIdQuery)] Guid? tenantId, CancellationToken cancellationToken)
    {
        var all = await clients.ListAsync(tenantId, cancellationToken);
        return new ClientsResponse(all.Select(ToResponse).ToList());
    }

    /// <summary>Returns one registered client.</summary>
    /// <param name="clientId">The client id as the identity provider issued it.</param>
    /// <param name="cancellationToken">Aborts reading the client.</param>
    /// <response code="200">The client.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    /// <response code="404">The client is not registered.</response>
    [HttpGet(ClientsApi.ClientPath)]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ClientResponse>> Get(string clientId, CancellationToken cancellationToken)
    {
        return ToActionResult(await clients.GetAsync(clientId, cancellationToken));
    }

    /// <summary>Registers a client of the identity provider for a tenant and a plan and records the change in the audit log.</summary>
    /// <param name="request">The client id, its tenant and its plan.</param>
    /// <param name="cancellationToken">Aborts the change.</param>
    /// <response code="201">The client was registered.</response>
    /// <response code="400">The client id is empty, too long or already registered, or the tenant or the plan does not exist; <c>errors</c> names the field.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    [HttpPost(ClientsApi.Path)]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status201Created, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ClientResponse>> Register(RegisterClientRequest request, CancellationToken cancellationToken)
    {
        var registered = await clients.RegisterAsync(request.ClientId, request.TenantId, request.PlanId, AdminHandler.OperatorOf(User), cancellationToken);
        if (!registered.IsSuccess)
        {
            return ToActionResult(registered);
        }

        var client = registered.Value;
        logger.LogInformation("Client {ClientId} registered for tenant {TenantId}", client.ClientId, client.TenantId);
        return CreatedAtAction(nameof(Get), new { clientId = client.ClientId }, ToResponse(client));
    }

    /// <summary>Holds a client to another plan and records the change in the audit log.</summary>
    /// <param name="clientId">The client id as the identity provider issued it.</param>
    /// <param name="request">The client's new plan.</param>
    /// <param name="cancellationToken">Aborts the change.</param>
    /// <response code="200">The client, changed; unchanged and not audited when the plan was its own.</response>
    /// <response code="400">The plan does not exist; <c>errors.planId</c> says so.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    /// <response code="404">The client is not registered.</response>
    [HttpPut(ClientsApi.ClientPath)]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ClientResponse>> ChangePlan(string clientId, ChangeClientPlanRequest request, CancellationToken cancellationToken)
    {
        var changed = await clients.ChangePlanAsync(clientId, request.PlanId, AdminHandler.OperatorOf(User), cancellationToken);
        if (changed.IsSuccess)
        {
            logger.LogInformation("Client {ClientId} moved to plan {PlanId}", clientId, request.PlanId);
        }

        return ToActionResult(changed);
    }

    /// <summary>
    /// Removes a client and records the change in the audit log. The client
    /// stays at the identity provider, and its usage with its tenant.
    /// </summary>
    /// <param name="clientId">The client id as the identity provider issued it.</param>
    /// <param name="cancellationToken">Aborts the change.</param>
    /// <response code="204">The client was removed.</response>
    /// <response code="401">No valid gateway token for the control plane.</response>
    /// <response code="403">The token is not the operator's.</response>
    /// <response code="404">The client is not registered.</response>
    [HttpDelete(ClientsApi.ClientPath)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Remove(string clientId, CancellationToken cancellationToken)
    {
        var removed = await clients.RemoveAsync(clientId, AdminHandler.OperatorOf(User), cancellationToken);
        if (!removed.IsSuccess)
        {
            return ToActionResult(removed).Result!;
        }

        logger.LogInformation("Client {ClientId} removed", clientId);
        return NoContent();
    }

    private ActionResult<ClientResponse> ToActionResult(Result<ClientOverview> result)
    {
        if (result.IsSuccess)
        {
            return ToResponse(result.Value);
        }

        logger.LogDebug("Client request refused: {ErrorCode}", result.Error.Code);
        if (result.Error.Code == ClientAdministration.NotFoundCode)
        {
            return Problem(detail: result.Error.Message, statusCode: StatusCodes.Status404NotFound);
        }

        var fields = new ModelStateDictionary();
        fields.AddModelError(FieldOf(result.Error.Code), result.Error.Message);
        return ValidationProblem(fields);
    }

    private static string FieldOf(string errorCode) => errorCode switch
    {
        Client.ClientIdRequiredCode or Client.ClientIdTooLongCode or Client.ClientIdTakenCode => ClientsApi.ClientIdField,
        Client.TenantUnknownCode => ClientsApi.TenantIdField,
        Client.PlanUnknownCode => ClientsApi.PlanIdField,
        _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, "Every error of a client change names its field."),
    };

    private static ClientResponse ToResponse(ClientOverview client)
    {
        return new ClientResponse(client.ClientId, client.TenantId, client.TenantName, client.PlanId, client.PlanName);
    }
}
