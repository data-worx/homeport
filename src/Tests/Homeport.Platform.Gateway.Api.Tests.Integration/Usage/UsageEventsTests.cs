// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Homeport.Platform.Gateway.Domain.Usage;
using Homeport.Platform.Gateway.Infrastructure.Usage;
using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Wolverine.Tracking;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Usage;

public sealed class UsageEventsTests
{
    // Each test tracks what the gateway publishes while it sends its own
    // requests, traced with a trace id of their own.
    public sealed class InvokeAsync(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private const string ParentSpanId = "00f067aa0ba902b7";

        private const string UsageEventsCategory = "Homeport.Platform.Gateway.Api.Usage.UsageEvents";

        [Fact]
        public async Task Publishes_The_Tenant_Client_And_Route_Of_A_Forwarded_Request()
        {
            var traceId = NewTraceId();

            var usage = Assert.Single(await PublishedAsync(gateway.CreateClient(), traceId, ForwardingGateway.RequestPath));

            Assert.Equal(
                (StandInControlPlane.KnownTenantId, StandInControlPlane.KnownClientId, "echo", ForwardingGateway.RequestPath, "requests", 1),
                (usage.TenantId, usage.ClientId, usage.ModuleId, usage.Route, usage.Meter, usage.Quantity));
        }

        [Fact]
        public async Task Publishes_The_Status_The_Client_Got()
        {
            var traceId = NewTraceId();

            var usage = Assert.Single(await PublishedAsync(gateway.CreateClient(), traceId, ForwardingGateway.NotFoundProblemPath));

            Assert.Equal(404, usage.StatusCode);
        }

        [Fact]
        public async Task Publishes_The_Quantity_The_Module_Reported()
        {
            var usage = Assert.Single(await PublishedAsync(gateway.CreateClient(), NewTraceId(), Reporting("requests=5")));

            Assert.Equal(5, usage.Quantity);
        }

        [Fact]
        public async Task Publishes_The_Route_Default_When_The_Report_Is_Rejected()
        {
            var usage = Assert.Single(await PublishedAsync(gateway.CreateClient(), NewTraceId(), Reporting("documents=5")));

            Assert.Equal(("requests", 1), (usage.Meter, usage.Quantity));
        }

        [Fact]
        public async Task Counts_A_Rejected_Report_With_The_Rule_It_Broke()
        {
            using var rejected = new MetricCollector<long>(
                gateway.Host.Services.GetRequiredService<IMeterFactory>(), UsageMetrics.MeterName, UsageMetrics.RejectedInstrument);

            await PublishedAsync(gateway.CreateClient(), NewTraceId(), Reporting("requests=0"));

            Assert.Equal(ReportedUsage.InvalidQuantityCode, Assert.Single(rejected.GetMeasurementSnapshot()).Tags[UsageMetrics.ReasonTag]);
        }

        [Fact]
        public async Task Logs_A_Rejected_Report_As_A_Warning()
        {
            var traceId = NewTraceId();

            await PublishedAsync(gateway.CreateClient(), traceId, Reporting("requests=five"));

            Assert.Contains(
                gateway.Logs.GetSnapshot(),
                entry => entry.Category == UsageEventsCategory
                    && entry.Level == LogLevel.Warning
                    && entry.Message.Contains(ReportedUsage.InvalidQuantityCode, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Publishes_A_New_Event_Id_Per_Request()
        {
            var first = Assert.Single(await PublishedAsync(gateway.CreateClient(), NewTraceId(), ForwardingGateway.RequestPath));

            var second = Assert.Single(await PublishedAsync(gateway.CreateClient(), NewTraceId(), ForwardingGateway.RequestPath));

            Assert.NotEqual(first.EventId, second.EventId);
        }

        [Fact]
        public async Task Publishes_Nothing_For_A_Request_Without_A_Token()
        {
            var published = await PublishedAsync(gateway.CreateAnonymousClient(), NewTraceId(), ForwardingGateway.RequestPath);

            Assert.Empty(published);
        }

        // Tracking returns once the gateway has handed off everything the
        // request made it publish.
        private async Task<IReadOnlyList<UsageRecorded>> PublishedAsync(HttpClient client, string traceId, string path)
        {
            var session = await gateway.Host.TrackActivity()
                .ExecuteAndWaitAsync(async Task (_) =>
                {
                    using var request = new HttpRequestMessage(HttpMethod.Get, "/modules/echo" + path);
                    request.Headers.Add("traceparent", $"00-{traceId}-{ParentSpanId}-01");
                    using var response = await client.SendAsync(request);
                });

            return session.Sent.MessagesOf<UsageRecorded>().Where(usage => usage.TraceId == traceId).ToList();
        }

        private static string Reporting(string report) =>
            $"{ForwardingGateway.UsagePath}?{ForwardingGateway.ReportQuery}={Uri.EscapeDataString(report)}";

        private static string NewTraceId() => ActivityTraceId.CreateRandom().ToHexString();
    }
}
