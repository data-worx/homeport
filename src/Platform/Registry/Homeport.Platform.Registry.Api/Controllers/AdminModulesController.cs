// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.Registry.Application;
using Homeport.Shared.Client.Platform.Registry.V1;
using Homeport.Shared.Internal.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Homeport.Platform.Registry.Api.Controllers;

/// <summary>
/// The operator reads the registered modules. Reached through the gateway's
/// admin endpoint at <c>/platform/v1/admin/modules</c>, with a caller token
/// of scope <c>admin</c> (ADR-0020). Registrations are platform data, not a
/// tenant's, so reading them writes no audit entry.
/// </summary>
[ApiController]
[Authorize(Policy = PlatformPolicies.Admin)]
public sealed class AdminModulesController(Registrations registrations) : ControllerBase
{
    /// <summary>Returns every module with at least one live instance, ordered by module id.</summary>
    /// <param name="cancellationToken">Aborts reading the modules.</param>
    /// <response code="200">The modules with their instances and the routes the gateway serves for them.</response>
    /// <response code="401">No valid gateway token for the registry.</response>
    /// <response code="403">The token is not the operator's: it lacks the scope <c>admin</c> or names a tenant.</response>
    [HttpGet(ModulesApi.Path)]
    [ProducesResponseType<ModulesResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<ModulesResponse>> List(CancellationToken cancellationToken)
    {
        var modules = await registrations.ModulesAsync(cancellationToken);
        return new ModulesResponse(modules.Select(ToResponse).ToList());
    }

    /// <summary>The wire type of a module.</summary>
    private static ModuleResponse ToResponse(RegisteredModule module)
    {
        return new ModuleResponse(
            module.ModuleId,
            module.Instances
                .Select(registered => new ModuleInstanceResponse(
                    registered.Instance.InstanceId,
                    registered.Instance.Address,
                    registered.Instance.Manifest.Version,
                    registered.Instance.Manifest.ContractVersion,
                    registered.LastHeartbeatAt,
                    registered.ExpiresAt))
                .ToList(),
            module.Routes
                .Select(route => new ModuleRouteResponse(route.Method, route.Path, route.Scope, route.Meter, route.Quantity, route.Version))
                .ToList());
    }
}
