// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Diagnostics.Metrics;
using Homeport.Platform.Gateway.Api.Telemetry;
using Homeport.Platform.Gateway.Infrastructure.Usage;
using Homeport.Shared.ServiceDefaults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Telemetry;

public sealed class ModuleRequestTelemetryTests
{
    // Each test sets the bucket it draws from and traces its request with a
    // trace id of its own; the tests of one class run one after another.
    public sealed class InvokeAsync(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private const string ModulePath = "/modules/echo" + ForwardingGateway.RequestPath;

        private const string AspNetCoreSource = "Microsoft.AspNetCore";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

        [Fact]
        public async Task Tags_The_Span_With_Tenant_Client_Module_And_Meter()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Full;
            var traceId = ActivityTraceId.CreateRandom();
            var span = new TaskCompletionSource<Activity>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var listener = Listen(traceId, span);

            using var response = await SendAsync(traceId);

            var tagged = await span.Task.WaitAsync(Timeout);
            Assert.Equal(
                (StandInControlPlane.KnownTenantId.ToString(), StandInControlPlane.KnownClientId, "echo", "requests"),
                (Tag(tagged, TelemetryAttributes.TenantId), Tag(tagged, TelemetryAttributes.ClientId), Tag(tagged, TelemetryAttributes.ModuleId), Tag(tagged, TelemetryAttributes.Meter)));
        }

        [Fact]
        public async Task Counts_A_Forwarded_Request_With_Its_Module_And_Status()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Full;
            using var requests = Collector();

            using var response = await SendAsync(ActivityTraceId.CreateRandom());

            var counted = await CountedAsync(requests);
            Assert.Equal(("echo", 200), (counted.Tags[TelemetryAttributes.ModuleId], counted.Tags[RequestMetrics.StatusCodeTag]));
        }

        [Fact]
        public async Task Counts_A_Throttled_Request_With_Its_Status()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Empty;
            using var requests = Collector();

            using var response = await SendAsync(ActivityTraceId.CreateRandom());

            var counted = await CountedAsync(requests);
            Assert.Equal(429, counted.Tags[RequestMetrics.StatusCodeTag]);
        }

        private async Task<HttpResponseMessage> SendAsync(ActivityTraceId traceId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, ModulePath);
            request.Headers.Add("traceparent", $"00-{traceId.ToHexString()}-00f067aa0ba902b7-01");
            return await gateway.CreateClient().SendAsync(request);
        }

        // The gateway's server span is the one of the trace that names a module;
        // the stand-in module's span, in the same process, names none.
        private static ActivityListener Listen(ActivityTraceId traceId, TaskCompletionSource<Activity> span)
        {
            var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == AspNetCoreSource,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
                ActivityStopped = activity =>
                {
                    if (activity.TraceId == traceId && activity.GetTagItem(TelemetryAttributes.ModuleId) is not null)
                    {
                        span.TrySetResult(activity);
                    }
                },
            };
            ActivitySource.AddActivityListener(listener);
            return listener;
        }

        private static string? Tag(Activity activity, string name) => activity.GetTagItem(name) as string;

        private MetricCollector<long> Collector()
        {
            return new MetricCollector<long>(
                gateway.Host.Services.GetRequiredService<IMeterFactory>(), UsageMetrics.MeterName, RequestMetrics.RequestsInstrument);
        }

        // Counted once the gateway has answered, which may be after the client has its response.
        private static async Task<CollectedMeasurement<long>> CountedAsync(MetricCollector<long> requests)
        {
            await requests.WaitForMeasurementsAsync(1, Timeout);
            return Assert.Single(requests.GetMeasurementSnapshot());
        }
    }
}
