// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.ControlPlane.Application;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Platform.ControlPlane.Api.Controllers;

/// <summary>
/// Which tenant and plan a client belongs to, for the gateway. Only the
/// gateway may ask, with a service token for the control plane (ADR-0014).
/// </summary>
[ApiController]
[Authorize]
public sealed class ClientsController(IClientLookup clients, ILogger<ClientsController> logger) : ControllerBase
{
    /// <summary>The tenant and the plan limits of a client.</summary>
    /// <param name="clientId">The client id as the identity provider issued it.</param>
    /// <param name="cancellationToken">Aborts the lookup.</param>
    /// <response code="200">The client's tenant and plan.</response>
    /// <response code="401">No valid service token for the control plane.</response>
    /// <response code="404">The control plane knows no such client.</response>
    [HttpGet(ControlPlaneService.ClientPath)]
    [ProducesResponseType<ClientResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ClientResponse>> Get(string clientId, CancellationToken cancellationToken)
    {
        var client = await clients.FindAsync(clientId, cancellationToken);
        if (client is null)
        {
            logger.LogDebug("Client {ClientId} is unknown", clientId);
            return NotFound();
        }

        var plan = client.Plan!;
        return new ClientResponse(client.ClientId, client.TenantId, new PlanResponse(plan.Id, plan.RequestsPerSecond, plan.Burst));
    }
}
