// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.Metrics;
using Homeport.Platform.Gateway.Application.Throttling;
using Homeport.Platform.Gateway.Domain.Throttling;
using Homeport.Platform.Gateway.Infrastructure.Throttling;
using Homeport.Platform.Gateway.Infrastructure.Usage;
using Homeport.Testing.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Integration.Throttling;

public sealed class ValkeyTokenBucketsTests
{
    public sealed class TakeAsync(ValkeyContainer valkey) : IClassFixture<ValkeyContainer>, IAsyncLifetime
    {
        private const int Parallel = 50;

        private const string TokensField = "tokens";

        private const string AtField = "at";

        // Microseconds since the epoch, in the year 2286.
        private const long FarFuture = 9_999_999_999_999_999;

        // Slow, so the bucket hardly refills while a test runs.
        private static readonly RateLimit Limit = new(1, 10);

        private IHost? host;

        private ITokenBuckets Buckets => (host ?? throw new InvalidOperationException("The host has not been started."))
            .Services.GetRequiredService<ITokenBuckets>();

        [Fact]
        public async Task Takes_A_Token_From_A_Full_Bucket()
        {
            var bucket = await Buckets.TakeAsync(Guid.NewGuid(), Limit, CancellationToken.None);

            Assert.Equal((true, Limit.Burst - 1, TimeSpan.Zero), (bucket.Value.Taken, bucket.Value.Remaining, bucket.Value.NextToken));
        }

        [Fact]
        public async Task Refuses_A_Token_From_An_Empty_Bucket()
        {
            var tenantId = Guid.NewGuid();
            await EmptyAsync(tenantId);

            var bucket = await Buckets.TakeAsync(tenantId, Limit, CancellationToken.None);

            Assert.Equal((false, 0), (bucket.Value.Taken, bucket.Value.Remaining));
        }

        [Fact]
        public async Task States_When_An_Empty_Bucket_Holds_A_Token_Again()
        {
            var tenantId = Guid.NewGuid();
            await EmptyAsync(tenantId);

            var bucket = await Buckets.TakeAsync(tenantId, Limit, CancellationToken.None);

            // One token at one per second.
            Assert.Equal(TimeSpan.FromSeconds(1), bucket.Value.NextToken);
        }

        [Fact]
        public async Task Never_Hands_Out_More_Tokens_Than_The_Bucket_Holds_To_Parallel_Calls()
        {
            var tenantId = Guid.NewGuid();
            var server = valkey.Connection.GetServers().Single();
            var before = await server.TimeAsync();

            var buckets = await Task.WhenAll(Enumerable.Range(0, Parallel)
                .Select(_ => Buckets.TakeAsync(tenantId, Limit, CancellationToken.None)));
            var after = await server.TimeAsync();

            // What the bucket held, plus what it refilled while the calls ran by
            // Valkey's clock, the one the script refills by, which may be stepped.
            var allowed = Limit.Burst + (int)Math.Ceiling(Math.Max(0, (after - before).TotalSeconds) * Limit.RequestsPerSecond);
            Assert.InRange(buckets.Count(bucket => bucket.Value.Taken), Limit.Burst, allowed);
        }

        [Fact]
        public async Task Keeps_A_Bucket_Per_Tenant()
        {
            var tenantId = Guid.NewGuid();
            for (var i = 0; i < Limit.Burst; i++)
            {
                await Buckets.TakeAsync(tenantId, Limit, CancellationToken.None);
            }

            var other = await Buckets.TakeAsync(Guid.NewGuid(), Limit, CancellationToken.None);

            Assert.True(other.Value.Taken);
        }

        [Fact]
        public async Task Lets_The_Bucket_Expire_Once_It_Would_Be_Full()
        {
            var tenantId = Guid.NewGuid();

            await Buckets.TakeAsync(tenantId, Limit, CancellationToken.None);

            var timeToLive = await valkey.Connection.GetDatabase().KeyTimeToLiveAsync(ValkeyTokenBuckets.KeyPrefix + tenantId.ToString("D"));
            Assert.InRange(timeToLive ?? TimeSpan.Zero, TimeSpan.FromMilliseconds(1), TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task Returns_Unavailable_And_Counts_It_While_Valkey_Is_Stopped()
        {
            var stopped = new RedisBuilder(ContainerImages.Valkey).Build();
            await stopped.StartAsync();
            var connectionString = stopped.GetConnectionString();
            await stopped.DisposeAsync();
            var unreachable = await StartAsync(connectionString);
            using var counted = new MetricCollector<long>(
                unreachable.Services.GetRequiredService<IMeterFactory>(), UsageMetrics.MeterName, ThrottlingMetrics.UnavailableInstrument);

            var bucket = await unreachable.Services.GetRequiredService<ITokenBuckets>().TakeAsync(Guid.NewGuid(), Limit, CancellationToken.None);

            await StopAsync(unreachable);
            Assert.Equal((ITokenBuckets.UnavailableCode, 1L), (bucket.Error.Code, counted.GetMeasurementSnapshot().EvaluateAsCounter()));
        }

        public async Task InitializeAsync()
        {
            await valkey.FlushAsync();
            host = await StartAsync(valkey.ConnectionString);
        }

        public async Task DisposeAsync()
        {
            if (host is not null)
            {
                await StopAsync(host);
            }
        }

        // Writes an empty bucket last refilled in the future, so the script, which
        // counts no time before that, refills nothing however long the test takes
        // and however Valkey's clock is stepped. The fields are the script's.
        private async Task EmptyAsync(Guid tenantId)
        {
            await valkey.Connection.GetDatabase().HashSetAsync(
                ValkeyTokenBuckets.KeyPrefix + tenantId.ToString("D"),
                [new HashEntry(TokensField, 0), new HashEntry(AtField, FarFuture)]);
        }

        private static async Task<IHost> StartAsync(string connectionString)
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{ThrottlingServiceCollectionExtensions.ValkeyConnectionName}"] = connectionString,
            });
            builder.AddThrottling();

            var started = builder.Build();
            await started.StartAsync();
            return started;
        }

        private static async Task StopAsync(IHost stopping)
        {
            await stopping.StopAsync();
            stopping.Dispose();
        }
    }
}
