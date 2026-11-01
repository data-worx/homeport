// SPDX-License-Identifier: AGPL-3.0-or-later

using StackExchange.Redis;
using Testcontainers.Redis;

namespace Homeport.Platform.Registry.Infrastructure.Tests.Integration;

/// <summary>
/// A throwaway Valkey, the image the AppHost runs, started once for the tests
/// that share it. Needs a running Docker.
/// </summary>
public sealed class ValkeyContainer : IAsyncLifetime
{
    // Pinned like the AppHost pins it.
    private const string Image = "valkey/valkey:9.1@sha256:418652cfb58ef879d4978c33553735d7147016032d5aefaa14c828e611eb9dfd";

    private readonly RedisContainer container = new RedisBuilder(Image).Build();
    private ConnectionMultiplexer? connection;

    internal IConnectionMultiplexer Connection =>
        connection ?? throw new InvalidOperationException("Valkey has not been started.");

    /// <summary>Removes every key, so a test starts from an empty Valkey.</summary>
    internal async Task FlushAsync()
    {
        foreach (var server in Connection.GetServers())
        {
            await server.FlushDatabaseAsync();
        }
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
