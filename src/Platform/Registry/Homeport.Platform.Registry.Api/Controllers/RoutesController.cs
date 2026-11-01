// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using System.Security.Cryptography;
using System.Text.Json;
using Homeport.Platform.Registry.Application;
using Homeport.Platform.Registry.Domain.Routing;
using Homeport.Shared.Internal.Platform.Registry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Homeport.Platform.Registry.Api.Controllers;

/// <summary>
/// The routes of the live instances, for the gateway. Only the gateway may
/// read them, with a service token for the registry (ADR-0014).
/// </summary>
[ApiController]
[Authorize]
public sealed class RoutesController(Registrations registrations) : ControllerBase
{
    /// <summary>The routes of every module with at least one live instance.</summary>
    /// <remarks>
    /// The <c>ETag</c> changes exactly when the routes do, so a poll with
    /// <c>If-None-Match</c> is answered with <c>304</c> until something changed.
    /// </remarks>
    /// <param name="cancellationToken">Aborts reading the routes.</param>
    /// <response code="200">The routes, with an <c>ETag</c>.</response>
    /// <response code="304">The routes have not changed since the <c>ETag</c> in <c>If-None-Match</c>.</response>
    /// <response code="401">No valid service token for the registry.</response>
    [HttpGet(RegistryService.RoutesPath)]
    [ProducesResponseType<RouteTableResponse>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<RouteTableResponse>> Get(CancellationToken cancellationToken)
    {
        var routes = ToResponse(await registrations.RoutesAsync(cancellationToken));
        var etag = ETagOf(routes);

        if (Request.GetTypedHeaders().IfNoneMatch.Any(match => match.Compare(etag, useStrongComparison: true)))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.GetTypedHeaders().ETag = etag;
        return routes;
    }

    // The table is ordered, so the same routes always serialize to the same bytes.
    private static EntityTagHeaderValue ETagOf(RouteTableResponse routes)
    {
        var hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(routes));
        return new EntityTagHeaderValue($"\"{Convert.ToHexStringLower(hash)}\"");
    }

    private static RouteTableResponse ToResponse(IReadOnlyList<ModuleRoutes> modules)
    {
        return new RouteTableResponse(modules
            .Select(module => new ModuleRoutesResponse(
                module.ModuleId,
                module.Routes
                    .Select(route => new RouteResponse(route.Method, route.Path, route.Scope, route.Meter, route.Quantity, route.Instances))
                    .ToList()))
            .ToList());
    }
}
