// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Application.Throttling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

namespace Homeport.Platform.Gateway.Infrastructure.Throttling;

/// <summary>Wires the gateway's rate limit per tenant, with its token buckets in Valkey (ADR-0007).</summary>
public static class ThrottlingServiceCollectionExtensions
{
    /// <summary>The connection name of Valkey, as the AppHost declares it.</summary>
    public const string ValkeyConnectionName = "valkey";

    /// <summary>
    /// Adds <see cref="RequestThrottle"/> on buckets in Valkey, connected
    /// while the gateway starts (<see cref="ValkeyConnection"/>). The gateway
    /// fails open, so Valkey adds no health check, may be unreachable at
    /// start, and a command fails at once while it is down rather than
    /// waiting in a backlog: the gateway stays ready and fast without it.
    /// </summary>
    public static IHostApplicationBuilder AddThrottling(this IHostApplicationBuilder builder)
    {
        builder.AddRedisClient(
            ValkeyConnectionName,
            settings => settings.DisableHealthChecks = true,
            options =>
            {
                options.AbortOnConnectFail = false;
                options.BacklogPolicy = BacklogPolicy.FailFast;
            });

        builder.Services.TryAddSingleton(TimeProvider.System);
        builder.Services.AddSingleton<ThrottlingMetrics>();
        builder.Services.AddSingleton<ITokenBuckets, ValkeyTokenBuckets>();
        builder.Services.AddSingleton<RequestThrottle>();
        builder.Services.AddHostedService<ValkeyConnection>();

        return builder;
    }
}
