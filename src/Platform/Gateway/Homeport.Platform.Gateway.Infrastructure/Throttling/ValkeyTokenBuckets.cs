// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Application.Throttling;
using Homeport.Platform.Gateway.Domain.Throttling;
using Homeport.Shared.Contracts;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Homeport.Platform.Gateway.Infrastructure.Throttling;

/// <summary>
/// Keeps every tenant's token bucket in Valkey and draws from it with
/// <c>token-bucket.lua</c>, so all gateway replicas share one bucket per
/// tenant. While Valkey cannot be reached, each request is counted as
/// <see cref="ThrottlingMetrics.UnavailableInstrument"/> and passes; the
/// warning is logged at most once per <see cref="OutageWarnings.Interval"/>.
/// </summary>
internal sealed class ValkeyTokenBuckets(
    IConnectionMultiplexer valkey,
    ThrottlingMetrics metrics,
    TimeProvider time,
    ILogger<ValkeyTokenBuckets> logger) : ITokenBuckets
{
    /// <summary>The prefix of every key the gateway's throttling writes.</summary>
    internal const string KeyPrefix = "homeport:gateway:ratelimit:";

    private const string ScriptResource = "Homeport.Platform.Gateway.Infrastructure.Throttling.token-bucket.lua";

    private static readonly string Script = ReadScript();

    private readonly OutageWarnings warnings = new(time);

    public async Task<Result<BucketState>> TakeAsync(Guid tenantId, RateLimit limit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(limit);

        RedisResult result;
        try
        {
            result = await valkey.GetDatabase().ScriptEvaluateAsync(
                Script,
                [new RedisKey(KeyPrefix + tenantId.ToString("D"))],
                [limit.Burst, limit.RequestsPerSecond]);
        }
        catch (Exception exception) when (exception is RedisConnectionException or RedisTimeoutException)
        {
            metrics.Unavailable();
            if (warnings.TryWarn())
            {
                logger.LogWarning(exception, "Valkey could not be reached; requests pass without a rate limit (logged at most every {WarningInterval})", OutageWarnings.Interval);
            }

            return Result<BucketState>.Failure(new Error(ITokenBuckets.UnavailableCode, "Valkey could not be reached."));
        }

        var values = (long[])result!;
        return Result<BucketState>.Success(new BucketState(
            values[0] == 1,
            (int)values[1],
            TimeSpan.FromMilliseconds(values[2]),
            TimeSpan.FromMilliseconds(values[3])));
    }

    private static string ReadScript()
    {
        using var stream = typeof(ValkeyTokenBuckets).Assembly.GetManifestResourceStream(ScriptResource)
            ?? throw new InvalidOperationException($"The script '{ScriptResource}' is not embedded.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
