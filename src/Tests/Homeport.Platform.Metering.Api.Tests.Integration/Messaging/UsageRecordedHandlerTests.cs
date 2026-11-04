// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.Metrics;
using Homeport.Platform.Metering.Api.Messaging;
using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Homeport.Platform.Metering.Api.Tests.Integration.Messaging;

public sealed class UsageRecordedHandlerTests
{
    // Each test publishes for a tenant of its own, so their aggregates never mix.
    public sealed class Handle(RunningMetering metering) : IClassFixture<RunningMetering>
    {
        [Fact]
        public async Task Adds_The_Quantity_To_The_Hourly_And_Daily_Aggregate()
        {
            var message = Usage(Guid.NewGuid(), quantity: 3);

            await metering.PublishAsync(message);

            Assert.Equal((3L, 3L), await metering.QuantitiesAsync(message.TenantId));
        }

        [Fact]
        public async Task Counts_An_Event_Delivered_Twice_Once()
        {
            var message = Usage(Guid.NewGuid(), quantity: 3);
            await metering.PublishAsync(message);

            await metering.PublishAsync(message);

            Assert.Equal((3L, 3L), await metering.QuantitiesAsync(message.TenantId));
        }

        [Fact]
        public async Task Adds_Up_Two_Events_Of_One_Hour()
        {
            var tenantId = Guid.NewGuid();
            await metering.PublishAsync(Usage(tenantId, quantity: 3));

            await metering.PublishAsync(Usage(tenantId, quantity: 2));

            Assert.Equal((5L, 5L), await metering.QuantitiesAsync(tenantId));
        }

        [Fact]
        public async Task Counts_A_New_Event_As_Recorded()
        {
            using var events = Collector();

            await metering.PublishAsync(Usage(Guid.NewGuid(), quantity: 1));

            Assert.Equal(MeteringMetrics.Recorded, Assert.Single(events.GetMeasurementSnapshot()).Tags[MeteringMetrics.OutcomeTag]);
        }

        [Fact]
        public async Task Counts_A_Duplicate_As_Duplicate()
        {
            var message = Usage(Guid.NewGuid(), quantity: 1);
            await metering.PublishAsync(message);
            using var events = Collector();

            await metering.PublishAsync(message);

            Assert.Equal(MeteringMetrics.Duplicate, Assert.Single(events.GetMeasurementSnapshot()).Tags[MeteringMetrics.OutcomeTag]);
        }

        private MetricCollector<long> Collector()
        {
            return new MetricCollector<long>(
                metering.Services.GetRequiredService<IMeterFactory>(),
                MeteringMetrics.MeterName,
                MeteringMetrics.EventsInstrument);
        }

        private static UsageRecorded Usage(Guid tenantId, int quantity)
        {
            return new UsageRecorded(
                Guid.NewGuid(),
                DateTimeOffset.Parse("2026-11-04T13:30:00Z"),
                tenantId,
                "local-client",
                "echo",
                "/v1/echo",
                "requests",
                quantity,
                200,
                12,
                "0af7651916cd43dd8448eb211c80319c");
        }
    }
}
