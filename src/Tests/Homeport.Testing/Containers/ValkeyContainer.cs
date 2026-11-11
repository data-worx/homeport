// SPDX-License-Identifier: AGPL-3.0-or-later

using StackExchange.Redis;
using Testcontainers.Redis;

namespace Homeport.Testing.Containers;

/// <summary>
/// A throwaway Valkey, the image the AppHost runs, started once for the tests
/// that share it. Needs a running Docker.
/// </summary>
public sealed class ValkeyContainer : IAsyncLifetime
{
    private readonly RedisContainer container = new RedisBuilder(ContainerImages.Valkey).Build();
    private ConnectionMultiplexer? connection;

    /// <summary>The connection string a component under test connects with.</summary>
    public string ConnectionString => container.GetConnectionString();

    public IConnectionMultiplexer Connection =>
        connection ?? throw new InvalidOperationException("Valkey has not been started.");

    /// <summary>Removes every key, so a test starts from an empty Valkey.</summary>
    public async Task FlushAsync()
    {
        foreach (var server in Connection.GetServers())
        {
            await server.FlushDatabaseAsync();
        }
    }

    /// <summary>The remaining lifetime of the one key Valkey holds.</summary>
    public async Task<TimeSpan?> TimeToLiveAsync()
    {
        var key = Connection.GetServers().SelectMany(server => server.Keys()).Single();
        return await Connection.GetDatabase().KeyTimeToLiveAsync(key);
    }

    public async Task InitializeAsync()
    {
        await container.StartAsync();
        connection = await ConnectionMultiplexer.ConnectAsync($"{container.GetConnectionString()},allowAdmin=true");
    }

    public async Task DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        await container.DisposeAsync();
    }
}
