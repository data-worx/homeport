// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Globalization;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Platform.Gateway.Domain.Usage;
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
/// is not counted. The quantity is the route's default, unless the module
/// reported one that <see cref="ReportedUsage"/> accepts.
/// </summary>
internal sealed class UsageEvents(RequestDelegate next, TimeProvider time, UsageMetrics metrics, ILogger<UsageEvents> logger)
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
            Quantity(context, route),
            context.Response.StatusCode,
            (long)time.GetElapsedTime(started).TotalMilliseconds,
            Activity.Current?.TraceId.ToHexString() ?? context.TraceIdentifier));
    }

    private int Quantity(HttpContext context, IReadOnlyDictionary<string, string> route)
    {
        var meter = route[ModuleRoutes.MeterMetadata];
        var fallback = int.Parse(route[ModuleRoutes.QuantityMetadata], CultureInfo.InvariantCulture);
        if (context.Features.Get<UsageReport>() is not { } report)
        {
            return fallback;
        }

        var reported = ReportedUsage.QuantityOf(report.Value, meter);
        if (reported.IsSuccess)
        {
            return reported.Value;
        }

        logger.LogWarning(
            "Module {ModuleId} reported a quantity of {Meter} on {Route} that was rejected ({Reason}: {Message}); counted {Quantity} instead",
            route[ModuleRoutes.ModuleIdMetadata],
            meter,
            route[ModuleRoutes.PathMetadata],
            reported.Error.Code,
            reported.Error.Message,
            fallback);
        metrics.Rejected(reported.Error.Code);

        return fallback;
    }
}
