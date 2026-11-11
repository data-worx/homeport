// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.Metrics;
using System.Text;
using Homeport.Platform.Gateway.Infrastructure.Usage;
using Homeport.Shared.Internal.Platform.Metering;
using Homeport.Testing.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using Testcontainers.RabbitMq;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Integration.Usage;

public sealed class UsagePublisherTests
{
    // Each test starts a broker and a gateway host of its own: one removes the exchange.
    public sealed class PublishAsync : IAsyncLifetime
    {
        private const string Queue = "test.usage";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(50);

        private readonly RabbitMqContainer rabbitMq = new RabbitMqBuilder(ContainerImages.RabbitMq).Build();
        private IHost? host;
        private IConnection? connection;
        private IChannel? channel;

        private IHost Host => host ?? throw new InvalidOperationException("The host has not been started.");

        private IChannel Channel => channel ?? throw new InvalidOperationException("The broker has not been connected.");

        [Fact]
        public async Task Delivers_The_Event_To_A_Queue_Bound_To_The_Exchange()
        {
            await Channel.QueueDeclareAsync(Queue, durable: false, exclusive: true, autoDelete: true);
            await Channel.QueueBindAsync(Queue, UsageMessaging.Exchange, routingKey: string.Empty);
            var usage = Event();

            await PublishThroughScopeAsync(usage);

            Assert.Contains(usage.EventId.ToString(), await ReceiveAsync());
        }

        [Fact]
        public async Task Counts_An_Event_The_Broker_Does_Not_Take_As_Dropped()
        {
            using var dropped = new MetricCollector<long>(
                Host.Services.GetRequiredService<IMeterFactory>(), UsageMetrics.MeterName, UsageMetrics.DroppedInstrument);
            await Channel.ExchangeDeleteAsync(UsageMessaging.Exchange);

            await PublishThroughScopeAsync(Event());

            await dropped.WaitForMeasurementsAsync(1, Timeout);
            Assert.Equal(UsageMetrics.RetriesExhausted, Assert.Single(dropped.GetMeasurementSnapshot()).Tags[UsageMetrics.ReasonTag]);
        }

        public async Task InitializeAsync()
        {
            await rabbitMq.StartAsync();

            var builder = Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"ConnectionStrings:{UsageServiceCollectionExtensions.ConnectionName}"] = rabbitMq.GetConnectionString(),

                // One immediate retry: a dropped event shows without waiting.
                [$"{UsageOptions.SectionName}:{nameof(UsageOptions.RetryDelays)}:0"] = TimeSpan.Zero.ToString(),
            });
            builder.AddUsagePublishing();
            host = builder.Build();
            await host.StartAsync();

            connection = await new ConnectionFactory { Uri = new Uri(rabbitMq.GetConnectionString()) }.CreateConnectionAsync();
            channel = await connection.CreateChannelAsync();
        }

        public async Task DisposeAsync()
        {
            if (channel is not null)
            {
                await channel.DisposeAsync();
            }

            if (connection is not null)
            {
                await connection.DisposeAsync();
            }

            if (host is not null)
            {
                await host.StopAsync();
                host.Dispose();
            }

            await rabbitMq.DisposeAsync();
        }

        private async Task PublishThroughScopeAsync(UsageRecorded usage)
        {
            await using var scope = Host.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<UsagePublisher>().PublishAsync(usage);
        }

        private async Task<string> ReceiveAsync()
        {
            using var timeout = new CancellationTokenSource(Timeout);
            while (true)
            {
                if (await Channel.BasicGetAsync(Queue, autoAck: true, timeout.Token) is { } message)
                {
                    return Encoding.UTF8.GetString(message.Body.Span);
                }

                await Task.Delay(Poll, timeout.Token);
            }
        }

        private static UsageRecorded Event()
        {
            return new UsageRecorded(
                Guid.NewGuid(),
                DateTimeOffset.Parse("2026-11-04T13:30:00Z"),
                Guid.Parse("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61"),
                "local-client",
                "echo",
                "/v1/echo",
                "requests",
                1,
                200,
                12,
                "0af7651916cd43dd8448eb211c80319c");
        }
    }
}
