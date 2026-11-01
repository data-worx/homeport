// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Homeport.Platform.Registry.Application;
using Homeport.Platform.Registry.Domain.Routing;
using StackExchange.Redis;

namespace Homeport.Platform.Registry.Infrastructure.Instances;

/// <summary>
/// Keeps every live instance as one Valkey key with a time to live, so an
/// instance without heartbeats disappears on its own and every registry
/// replica sees the same instances.
/// </summary>
internal sealed class ValkeyInstanceStore(IConnectionMultiplexer valkey) : IInstanceStore
{
    /// <summary>The prefix of every key the registry writes.</summary>
    internal const string KeyPrefix = "homeport:registry:instances:";

    public async Task SaveAsync(Instance instance, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        var value = JsonSerializer.SerializeToUtf8Bytes(instance);
        await valkey.GetDatabase().StringSetAsync(Key(instance.Manifest.Id, instance.InstanceId), value, lifetime);
    }

    public Task<bool> RenewAsync(string moduleId, string instanceId, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        return valkey.GetDatabase().KeyExpireAsync(Key(moduleId, instanceId), lifetime);
    }

    public async Task<IReadOnlyList<Instance>> ListAsync(CancellationToken cancellationToken)
    {
        var keys = new List<RedisKey>();
        foreach (var server in valkey.GetServers().Where(server => !server.IsReplica))
        {
            await foreach (var key in server.KeysAsync(pattern: $"{KeyPrefix}*").WithCancellation(cancellationToken))
            {
                keys.Add(key);
            }
        }

        if (keys.Count == 0)
        {
            return [];
        }

        // A key that expired between listing and reading comes back empty.
        var values = await valkey.GetDatabase().StringGetAsync([.. keys]);

        return values
            .Where(value => value.HasValue)
            .Select(value => JsonSerializer.Deserialize<Instance>((byte[])value!)!)
            .ToList();
    }

    private static string Key(string moduleId, string instanceId)
    {
        return $"{KeyPrefix}{moduleId}:{instanceId}";
    }
}
