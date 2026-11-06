// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Homeport.Platform.Registry.Api.Authentication;
using Homeport.Platform.Registry.Api.Errors;
using Homeport.Platform.Registry.Application;
using Homeport.Platform.Registry.Domain.Manifests;
using Homeport.Platform.Registry.Domain.Routing;
using Homeport.Shared.Client.ModuleContract;
using Homeport.Shared.Client.Problems;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Homeport.Platform.Registry.Api.Controllers;

/// <summary>
/// Where module instances register and keep themselves registered. Every
/// request carries the module's registration key (ADR-0012).
/// </summary>
[ApiController]
[Authorize(AuthenticationSchemes = RegistrationKeyHandler.SchemeName)]
public sealed class InstancesController(Registrations registrations, ILogger<InstancesController> logger) : ControllerBase
{
    /// <summary>Registers an instance, or registers it again.</summary>
    /// <param name="moduleId">The module the instance belongs to; the manifest's <c>id</c> must match it.</param>
    /// <param name="instanceId">Chosen by the instance, unique per running instance.</param>
    /// <param name="request">Where the instance is reached and what it declares.</param>
    /// <param name="cancellationToken">Aborts the registration.</param>
    /// <response code="204">The instance is registered and routed to.</response>
    /// <response code="400">The request is not a registration.</response>
    /// <response code="401">Unknown module id or wrong registration key.</response>
    /// <response code="422">The manifest breaks the module contract; <c>errors</c> says where.</response>
    [HttpPut(RegistryPaths.Instance)]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Register(
        string moduleId,
        string instanceId,
        RegisterInstanceRequest request,
        CancellationToken cancellationToken)
    {
        var instance = new Instance(instanceId, request.Address, ToManifest(request.Manifest));
        var errors = await registrations.RegisterAsync(moduleId, instance, cancellationToken);

        if (errors.Count > 0)
        {
            logger.LogWarning(
                "Instance {InstanceId} of module {ModuleId} was not registered: its manifest breaks the module contract at {Fields}",
                instanceId,
                moduleId,
                errors.Keys);

            var fields = new ModelStateDictionary();
            foreach (var (field, messages) in errors)
            {
                foreach (var message in messages)
                {
                    fields.AddModelError(field, message);
                }
            }

            return ValidationProblem(statusCode: RegistryProblems.InvalidManifest.Status, modelStateDictionary: fields);
        }

        logger.LogInformation(
            "Instance {InstanceId} of module {ModuleId} {Version} registered at {Address} with {RouteCount} routes",
            instanceId,
            moduleId,
            instance.Manifest.Version,
            instance.Address,
            instance.Manifest.Routes.Count);

        return NoContent();
    }

    /// <summary>Keeps an instance registered for another 30 s.</summary>
    /// <param name="moduleId">The module the instance belongs to.</param>
    /// <param name="instanceId">The id the instance registered with.</param>
    /// <param name="cancellationToken">Aborts the heartbeat.</param>
    /// <response code="204">The instance stays registered.</response>
    /// <response code="401">Unknown module id or wrong registration key.</response>
    /// <response code="404">The registry no longer knows the instance; it registers again.</response>
    [HttpPost(RegistryPaths.Heartbeat)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    public async Task<IActionResult> Heartbeat(string moduleId, string instanceId, CancellationToken cancellationToken)
    {
        if (await registrations.RenewAsync(moduleId, instanceId, cancellationToken))
        {
            return NoContent();
        }

        logger.LogDebug("Heartbeat of unknown instance {InstanceId} of module {ModuleId}; it has to register again", instanceId, moduleId);
        return Problem(
            detail: $"Instance '{instanceId}' of module '{moduleId}' is not registered; register it again.",
            statusCode: StatusCodes.Status404NotFound);
    }

    private static Manifest ToManifest(ModuleManifest manifest)
    {
        var routes = manifest.Routes
            .Select(route => new Domain.Manifests.ManifestRoute(route.Method, route.Path, route.Scope, route.Meter, route.Quantity))
            .ToList();

        return new Manifest(manifest.Id, manifest.Name, manifest.Version, manifest.ContractVersion, routes, manifest.Scopes, manifest.Meters, manifest.OpenApiUrl);
    }
}
