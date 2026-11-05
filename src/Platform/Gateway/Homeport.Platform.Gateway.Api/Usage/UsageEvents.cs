// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Globalization;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Platform.Gateway.Infrastructure.Usage;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Homeport.Shared.Internal.Platform.Metering;
using Yarp.ReverseProxy.Model;

namespace Homeport.Platform.Gateway.Api.Usage;

/// <summary>
/// Publishes a <see cref="UsageRecorded"/> through <see cref="UsagePublisher"/> for
/// every request forwarded to a module, once the gateway has answered it,
/// whatever the status. Part of the proxy pipeline, so it sees only requests
/// of a known client on a module's route; one the gateway refuses before -
/// <c>401</c>, <c>403</c>, an unknown module - is not a module request and
/// is not counted.
/// </summary>
internal sealed class UsageEvents(RequestDelegate next, TimeProvider time)
{
    public async Task InvokeAsync(HttpContext context, UsagePublisher publisher)
    {
        var started = time.GetTimestamp();
        await next(context);

        // The client policy of every module route has set it.
        var client = context.Features.Get<ClientResponse>()
            ?? throw new InvalidOperationException("A request reached a module without a known client.");
        var route = context.GetReverseProxyFeature().Route.Config.Metadata
            ?? throw new InvalidOperationException("A module route carries no metadata.");

        await publisher.PublishAsync(new UsageRecorded(
            Guid.NewGuid(),
            time.GetUtcNow(),
            client.TenantId,
            client.ClientId,
            route[ModuleRoutes.ModuleIdMetadata],
            route[ModuleRoutes.PathMetadata],
            route[ModuleRoutes.MeterMetadata],
            int.Parse(route[ModuleRoutes.QuantityMetadata], CultureInfo.InvariantCulture),
            context.Response.StatusCode,
            (long)time.GetElapsedTime(started).TotalMilliseconds,
            Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier));
    }
}
