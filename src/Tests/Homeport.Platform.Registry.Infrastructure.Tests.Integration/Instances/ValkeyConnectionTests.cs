// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Testing.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Homeport.Platform.Registry.Infrastructure.Tests.Integration.Instances;

public sealed class ValkeyConnectionTests
{
    public sealed class StartAsync(ValkeyContainer valkey) : IClassFixture<ValkeyContainer>
    {
        private const string Category = "Homeport.Platform.Registry.Infrastructure.Instances.ValkeyConnection";

        // Nothing listens there, and the connect gives up quickly.
        private const string Unreachable = "127.0.0.1:1,connectTimeout=100";

        // Asked of Valkey rather than of the host, which would connect on being asked.
        [Fact]
        public async Task Has_Connected_To_Valkey_Once_The_Host_Started()
        {
            var before = await ClientsAsync();

            using var host = await StartedAsync(valkey.ConnectionString);

            var connected = await ClientsAsync();
            await host.StopAsync();
            Assert.InRange(connected, before + 1, int.MaxValue);
        }

        [Fact]
        public async Task Logs_At_Information_That_It_Is_Connected()
        {
            using var host = await StartedAsync(valkey.ConnectionString);

            var levels = Levels(host);
            await host.StopAsync();
            Assert.Equal([LogLevel.Information], levels);
        }

        [Fact]
        public async Task Starts_With_A_Warning_While_Valkey_Is_Unreachable()
        {
            using var host = await StartedAsync(Unreachable);

            var levels = Levels(host);
            await host.StopAsync();
            Assert.Equal([LogLevel.Warning], levels);
        }

        private async Task<int> ClientsAsync()
        {
            return (await valkey.Connection.GetServers().Single().ClientListAsync()).Length;
        }

        private static List<LogLevel> Levels(IHost host)
        {
            return host.Services.GetFakeLogCollector().GetSnapshot()
                .Where(entry => entry.Category == Category)
                .Select(entry => entry.Level)
                .ToList();
        }

        private static async Task<IHost> StartedAsync(string connectionString)
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{RegistryInfrastructureExtensions.ValkeyConnectionName}"] = connectionString,
            });
            builder.Logging.AddFakeLogging();
            builder.AddRegistryInfrastructure();

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
