// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using Homeport.Platform.Gateway.Api.Admin;
using Homeport.Platform.Gateway.Api.Errors;
using Homeport.Platform.Gateway.Application.Throttling;
using Homeport.Platform.Gateway.Domain.Throttling;
using Homeport.Platform.Gateway.Infrastructure.Throttling;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Microsoft.Net.Http.Headers;
using Yarp.ReverseProxy.Model;

namespace Homeport.Platform.Gateway.Api.Throttling;

/// <summary>
/// Holds every module request to the rate limit of its tenant's plan, first
/// in the proxy pipeline: a rejected request is answered with <c>429</c> and
/// <c>Retry-After</c>, never reaches the module and is not metered. Every
/// answer carries the RateLimit headers, unless the token buckets cannot be
/// reached and the request passes unthrottled. A request to an admin
/// endpoint is not throttled.
/// </summary>
internal sealed class RequestThrottling(RequestDelegate next, ThrottlingMetrics metrics, ILogger<RequestThrottling> logger)
{
    public async Task InvokeAsync(HttpContext context, RequestThrottle throttle, IProblemDetailsService problems)
    {
        // The operator acts for no tenant, so there is no plan to hold it to (ADR-0020).
        if (context.GetReverseProxyFeature().Route.Config.Metadata?.ContainsKey(AdminRoutes.AdminAudienceMetadata) == true)
        {
            await next(context);
            return;
        }

        // The client policy of every module route has set it.
        var client = context.Features.Get<ClientResponse>()
            ?? throw new InvalidOperationException("A request reached a module without a known client.");
        var limit = new RateLimit(client.Plan.RequestsPerSecond, client.Plan.Burst);

        var decision = await throttle.DecideAsync(client.TenantId, limit, context.RequestAborted);
        if (decision is null)
        {
            await next(context);
            return;
        }

        // Set once the module answered, so they replace any it sent.
        context.Response.OnStarting(() =>
        {
            RateLimitHeaders.Set(context.Response.Headers, decision);
            return Task.CompletedTask;
        });

        if (decision.Allowed)
        {
            await next(context);
            return;
        }

        logger.LogDebug("A request of tenant {TenantId} exceeded the rate limit of plan {PlanId} and was rejected", client.TenantId, client.Plan.PlanId);
        metrics.Throttled();

        var problem = GatewayProblems.RateLimited;
        context.Response.StatusCode = problem.Status;
        context.Response.Headers[HeaderNames.RetryAfter] = decision.RetryAfterSeconds.ToString(CultureInfo.InvariantCulture);
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails =
            {
                Type = problem.Type,
                Title = problem.Title,
                Status = problem.Status,
                Detail = string.Create(CultureInfo.InvariantCulture, $"The tenant exceeded the rate limit of its plan. Retry after {decision.RetryAfterSeconds} seconds."),
            },
        });
    }
}
