// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.Metrics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Homeport.Platform.Gateway.Infrastructure.Throttling;
using Homeport.Platform.Gateway.Infrastructure.Usage;
using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Wolverine.Tracking;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Throttling;

public sealed class RequestThrottlingTests
{
    // Each test sets the bucket it draws from; the tests of one class run one after another.
    public sealed class InvokeAsync(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private const string ModulePath = "/modules/echo" + ForwardingGateway.RequestPath;

        [Fact]
        public async Task Forwards_A_Request_Within_The_Limit_With_The_RateLimit_Headers()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Full;

            using var response = await gateway.CreateClient().GetAsync(ModulePath);

            Assert.Equal(
                (HttpStatusCode.OK, "\"tenant\";q=20;w=2", "\"tenant\";r=19;t=1"),
                (response.StatusCode, Header(response, "RateLimit-Policy"), Header(response, "RateLimit")));
        }

        [Fact]
        public async Task Answers_A_Request_Over_The_Limit_As_Rate_Limited_Problem()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Empty;

            using var response = await gateway.CreateClient().GetAsync(ModulePath);

            var problem = await response.Content.ReadFromJsonAsync<JsonObject>();
            Assert.Equal(
                (HttpStatusCode.TooManyRequests, "https://homeport.data-worx.de/problems/rate-limited", 429),
                (response.StatusCode, problem?["type"]?.GetValue<string>(), problem?["status"]?.GetValue<int>()));
        }

        [Fact]
        public async Task Tells_A_Rejected_Request_When_To_Retry_And_What_Is_Left()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Empty;

            using var response = await gateway.CreateClient().GetAsync(ModulePath);

            Assert.Equal(
                ("2", "\"tenant\";r=0;t=2"),
                (Header(response, "Retry-After"), Header(response, "RateLimit")));
        }

        [Fact]
        public async Task Counts_A_Rejected_Request_As_Throttled()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Empty;
            using var throttled = new MetricCollector<long>(
                gateway.Host.Services.GetRequiredService<IMeterFactory>(), UsageMetrics.MeterName, ThrottlingMetrics.ThrottledInstrument);

            using var response = await gateway.CreateClient().GetAsync(ModulePath);

            Assert.Equal(1, throttled.GetMeasurementSnapshot().EvaluateAsCounter());
        }

        [Fact]
        public async Task Does_Not_Meter_A_Rejected_Request()
        {
            gateway.Buckets.Next = StandInTokenBuckets.Empty;

            // Tracking returns once the gateway has handed off everything the request made it publish.
            var session = await gateway.Host.TrackActivity()
                .ExecuteAndWaitAsync(async Task (_) =>
                {
                    using var response = await gateway.CreateClient().GetAsync(ModulePath);
                });

            Assert.Empty(session.Sent.MessagesOf<UsageRecorded>());
        }

        [Fact]
        public async Task Passes_Requests_Unthrottled_And_Counts_Them_While_Valkey_Is_Unreachable()
        {
            await using var unthrottled = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host => host
                    .WithUsage()
                    .WithUnreachableValkey()
                    .WithSigningKey()
                    .WithRegistry(gateway.Registry)
                    .WithClients(gateway.IdentityProvider, gateway.ControlPlane)
                    .ConfigureLogging(logging => logging.AddFakeLogging()));
            await ForwardingGateway.WaitForRoutesAsync(unthrottled);
            using var unavailable = new MetricCollector<long>(
                unthrottled.Services.GetRequiredService<IMeterFactory>(), UsageMetrics.MeterName, ThrottlingMetrics.UnavailableInstrument);

            using var response = await unthrottled.CreateClient()
                .WithToken(gateway.IdentityProvider.Issue(StandInControlPlane.KnownClientId))
                .GetAsync(ModulePath);

            Assert.Equal(
                (HttpStatusCode.OK, false, 1L),
                (response.StatusCode, response.Headers.Contains("RateLimit"), unavailable.GetMeasurementSnapshot().EvaluateAsCounter()));
        }

        private static string? Header(HttpResponseMessage response, string name)
        {
            return response.Headers.TryGetValues(name, out var values) ? string.Join(",", values) : null;
        }
    }
}
