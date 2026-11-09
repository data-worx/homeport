// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Homeport.Shared.ServiceDefaults;
using Yarp.ReverseProxy.Model;

namespace Homeport.Platform.Gateway.Api.Telemetry;

/// <summary>
/// Tags the request's span with tenant, client, module and meter, and counts
/// the request in <see cref="RequestMetrics"/> once it is answered, whatever
/// the status, a throttled one included. The first step of the proxy
/// pipeline, so it sees only requests of a known client on a module's route;
/// a request on a platform route names no module and is left alone. The
/// module id and the meter come from the route table, never from the
/// request, so no label carries what a caller sent.
/// </summary>
internal sealed class ModuleRequestTelemetry(RequestDelegate next, RequestMetrics metrics)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var route = context.GetReverseProxyFeature().Route.Config.Metadata;
        if (route is null || !route.TryGetValue(ModuleRoutes.ModuleIdMetadata, out var moduleId))
        {
            await next(context);
            return;
        }

        // The client policy of every module route has set it.
        var client = context.Features.Get<ClientResponse>()
            ?? throw new InvalidOperationException("A request reached a module without a known client.");

        Activity.Current?
            .SetTag(TelemetryAttributes.TenantId, client.TenantId.ToString())
            .SetTag(TelemetryAttributes.ClientId, client.ClientId)
            .SetTag(TelemetryAttributes.ModuleId, moduleId)
            .SetTag(TelemetryAttributes.Meter, route[ModuleRoutes.MeterMetadata]);

        await next(context);

        metrics.Answered(moduleId, context.Response.StatusCode);
    }
}
