// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Homeport.Shared.Client.ModuleContract;
using Homeport.Shared.Client.Modules.Echo.V1;
using Homeport.Shared.Client.Platform.Metering.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualBasic.FileIO;

namespace Homeport.Aspire.AppHost.Tests.Integration;

public sealed class ProgramTests
{
    [Collection(AppHostCollection.Name)]
    public sealed class Main(RunningAppHost appHost)
    {
        private const string BatchPath = "/modules/echo/v1/echo/batch";

        // Far more than the seeded plan's bucket holds and refills while they run.
        private const int ThrottledRequests = 30;

        private static readonly TimeSpan ExportPoll = TimeSpan.FromMilliseconds(250);

        [Theory]
        [InlineData("postgres")]
        [InlineData("rabbitmq")]
        [InlineData("valkey")]
        [InlineData("keycloak")]
        [InlineData("registry")]
        [InlineData("echo")]
        [InlineData("gateway")]
        [InlineData("control-plane")]
        [InlineData("metering")]
        public async Task Starts_The_Resource_Healthy(string resource)
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);

            var state = await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync(resource, timeout.Token);

            Assert.Equal(resource, state.Resource.Name);
        }

        [Theory]
        [InlineData("control-plane")]
        [InlineData("metering")]
        public void Starts_The_Service_Only_After_Its_Migrate_Completed(string service)
        {
            var model = appHost.App.Services.GetRequiredService<DistributedApplicationModel>();

            var waits = model.Resources.Single(resource => resource.Name == service)
                .Annotations.OfType<WaitAnnotation>()
                .Select(wait => (wait.Resource.Name, wait.WaitType));

            Assert.Contains(($"{service}-migrate", WaitType.WaitForCompletion), waits);
        }

        // A migrate resource serves no HTTP. An endpoint of its own would
        // share the port of the service's launch profile with the service.
        [Theory]
        [InlineData("control-plane-migrate")]
        [InlineData("metering-migrate")]
        public void Gives_The_Migrate_Resource_No_Endpoint(string migrate)
        {
            var model = appHost.App.Services.GetRequiredService<DistributedApplicationModel>();

            var endpoints = model.Resources.Single(resource => resource.Name == migrate)
                .Annotations.OfType<EndpointAnnotation>();

            Assert.Empty(endpoints);
        }

        [Fact]
        public async Task Has_Completed_Migrate_Successfully_Once_The_Control_Plane_Is_Healthy()
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);
            await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync("control-plane", timeout.Token);

            appHost.App.ResourceNotifications.TryGetCurrentState("control-plane-migrate", out var migrate);

            Assert.Equal((KnownResourceStates.Finished, 0), (migrate?.Snapshot.State?.Text, migrate?.Snapshot.ExitCode));
        }

        // The throttling tests rely on the small plan this run configures
        // rather than the AppHost's default.
        [Fact]
        public async Task Seeds_The_Plan_The_Run_Configures()
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);
            await appHost.App.ResourceNotifications.WaitForResourceAsync("control-plane-migrate", KnownResourceStates.Finished, timeout.Token);

            appHost.App.ResourceNotifications.TryGetCurrentState("control-plane-migrate", out var migrate);
            var seed = migrate!.Snapshot.EnvironmentVariables
                .Where(variable => variable.Name is "Seed__RequestsPerSecond" or "Seed__Burst")
                .Select(variable => (variable.Name, variable.Value))
                .Order();

            Assert.Equal(
                [("Seed__Burst", RunningAppHost.SeededBurst.ToString(CultureInfo.InvariantCulture)), ("Seed__RequestsPerSecond", RunningAppHost.SeededRequestsPerSecond.ToString(CultureInfo.InvariantCulture))],
                seed);
        }

        [Fact]
        public void Exposes_Only_The_Gateway_And_Keycloak_Externally()
        {
            var model = appHost.App.Services.GetRequiredService<DistributedApplicationModel>();

            var external = model.Resources
                .Where(resource => resource.Annotations.OfType<EndpointAnnotation>().Any(endpoint => endpoint.IsExternal))
                .Select(resource => resource.Name)
                .Order(StringComparer.Ordinal);

            Assert.Equal(["gateway", "keycloak"], external);
        }

        [Fact]
        public async Task Links_The_Gateway_To_The_Api_Docs_It_Serves()
        {
            using var timeout = new CancellationTokenSource(RunningAppHost.StartTimeout);
            var gateway = await appHost.App.ResourceNotifications.WaitForResourceHealthyAsync("gateway", timeout.Token);
            var link = gateway.Snapshot.Urls.Single(url => url.DisplayProperties.DisplayName == "API docs");

            using var client = new HttpClient();
            var response = await client.GetAsync(new Uri(link.Url), timeout.Token);

            Assert.Equal((HttpStatusCode.OK, "text/html"), (response.StatusCode, response.Content.Headers.ContentType?.MediaType));
        }

        // Whether the developer certificate is trusted differs between machines,
        // so the model is asserted rather than the listener Keycloak opens. The
        // annotation is experimental in Aspire and the only place that says so.
        [Fact]
        public void Serves_Keycloak_Without_An_Https_Certificate()
        {
            var model = appHost.App.Services.GetRequiredService<DistributedApplicationModel>();

#pragma warning disable ASPIRECERTIFICATES001
            var certificates = model.Resources.Single(resource => resource.Name == "keycloak")
                .Annotations.OfType<HttpsCertificateAnnotation>()
                .Select(annotation => (annotation.Certificate, annotation.UseDeveloperCertificate));
#pragma warning restore ASPIRECERTIFICATES001

            Assert.Equal([(null, false)], certificates);
        }

        [Fact]
        public async Task Returns_The_Body_Echo_Received_Through_The_Gateway()
        {
            var gateway = await Gateway();
            using var content = new StringContent("""{"hello":"Homeport ⚓"}""", Encoding.UTF8, "application/json");

            var response = await gateway.PostAsync("/modules/echo/v1/echo", content);

            Assert.Equal("""{"hello":"Homeport ⚓"}""", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Answers_A_Client_Unknown_To_The_Control_Plane_With_Forbidden()
        {
            await Gateway();
            var unmapped = appHost.App.CreateHttpClient("gateway");
            unmapped.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await appHost.TokenAsync(RunningAppHost.UnmappedClient));
            using var content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await unmapped.PostAsync("/modules/echo/v1/echo", content);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // The aggregates are compared with the events of their bucket, which
        // the other tests' calls to Echo add to as well.
        [Fact]
        public async Task Counts_A_Call_To_Echo_In_The_Seeded_Tenants_Hourly_And_Daily_Usage()
        {
            var gateway = await Gateway();
            var traceId = ActivityTraceId.CreateRandom().ToHexString();
            using var request = new HttpRequestMessage(HttpMethod.Get, RunningAppHost.EchoRequestPath);
            request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");

            using var response = await gateway.SendAsync(request);

            var usage = await appHost.MeteredAsync(traceId);
            Assert.Equal((1, usage.EventsInHour, usage.EventsOnDay), (usage.Quantity, usage.Hourly, usage.Daily));
        }

        [Fact]
        public async Task Counts_A_Batch_Of_Five_Items_As_Five_In_The_Items_Usage()
        {
            var gateway = await Gateway();
            var traceId = ActivityTraceId.CreateRandom().ToHexString();
            using var request = new HttpRequestMessage(HttpMethod.Post, BatchPath)
            {
                Content = new StringContent("""{"items":[1,2,3,4,5]}""", Encoding.UTF8, "application/json"),
            };
            request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");

            using var response = await gateway.SendAsync(request);

            var usage = await appHost.MeteredAsync(traceId);
            Assert.Equal((5, usage.EventsInHour, usage.EventsOnDay), (usage.Quantity, usage.Hourly, usage.Daily));
        }

        // The other tests' calls to Echo, and the gateway's own probes, count in
        // the same hours, and a probe's event may reach metering after its
        // answer reached the test. So the export is compared with the events
        // metering holds once both agree, after three calls it has counted.
        // Two hours, so the hour may turn while the test runs.
        [Fact]
        public async Task Exports_The_Requests_Metering_Counted_For_Calls_To_Echo_In_Json()
        {
            var gateway = await Gateway();
            var hour = UsageHour();
            await CallEchoAndWaitForMeteringAsync(gateway, calls: 3);

            var (exported, recorded) = await ExportedAndRecordedAsync(gateway, hour, csv: false);

            Assert.InRange(recorded, 3, long.MaxValue);
            Assert.Equal(recorded, exported);
        }

        [Fact]
        public async Task Exports_The_Requests_Metering_Counted_For_Calls_To_Echo_In_Csv()
        {
            var gateway = await Gateway();
            var hour = UsageHour();
            await CallEchoAndWaitForMeteringAsync(gateway, calls: 3);

            var (exported, recorded) = await ExportedAndRecordedAsync(gateway, hour, csv: true);

            Assert.InRange(recorded, 3, long.MaxValue);
            Assert.Equal(recorded, exported);
        }

        [Fact]
        public async Task Removes_The_Usage_Report_Of_Echo_Before_The_Caller_Sees_It()
        {
            var gateway = await Gateway();
            using var content = new StringContent("""{"items":[1,2,3]}""", Encoding.UTF8, "application/json");

            using var response = await gateway.PostAsync(BatchPath, content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(response.Headers.Contains(ModuleHeaders.Usage));
        }

        [Fact]
        public async Task Reaches_Echo_With_A_Module_Token_For_The_Seeded_Tenant_And_Client()
        {
            var gateway = await Gateway();

            var caller = await gateway.GetFromJsonAsync<CallerResponse>("/modules/echo/v1/caller");

            Assert.Equal(RunningAppHost.SeededTenantId, caller?.TenantId);
            Assert.Equal(RunningAppHost.SeededClient.ClientId, caller?.ClientId);
        }

        [Fact]
        public async Task Answers_A_Request_Sent_To_Echo_Without_The_Gateway_With_Unauthorized()
        {
            await Gateway();
            var echo = appHost.App.CreateHttpClient("echo");
            using var content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await echo.PostAsync("/v1/echo", content);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Answers_A_Client_Without_Echo_Invoke_With_Forbidden()
        {
            await Gateway();
            var unscoped = appHost.App.CreateHttpClient("gateway");
            unscoped.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await appHost.TokenAsync(RunningAppHost.SeededClient));
            using var content = new StringContent("{}", Encoding.UTF8, "application/json");

            var response = await unscoped.PostAsync("/modules/echo/v1/echo", content);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Passes_No_Header_With_The_Identity_Provider_Token_To_Echo()
        {
            var gateway = await Gateway();
            var token = gateway.DefaultRequestHeaders.Authorization!.Parameter!;

            var received = await gateway.GetFromJsonAsync<ReceivedRequestResponse>(RunningAppHost.EchoRequestPath);

            Assert.DoesNotContain(received!.Headers.Values, value => value.Contains(token, StringComparison.Ordinal));
        }

        [Fact]
        public async Task Answers_Requests_Over_The_Seeded_Plans_Limit_With_Too_Many_Requests_And_Retry_After()
        {
            var gateway = await appHost.ThrottledGatewayRoutingEchoAsync();

            var responses = await Task.WhenAll(Enumerable.Range(0, ThrottledRequests)
                .Select(_ => gateway.GetAsync(RunningAppHost.EchoRequestPath)));

            var rejected = responses.Where(response => response.StatusCode == HttpStatusCode.TooManyRequests).ToList();
            Assert.NotEmpty(rejected);
            Assert.All(rejected, response => Assert.NotNull(response.Headers.RetryAfter?.Delta));
        }

        // Each replica alone would accept a bucketful; together they accept
        // what the one bucket in Valkey held, plus what it refilled meanwhile.
        [Fact]
        public async Task Accepts_No_More_Across_Both_Gateway_Replicas_Than_The_Seeded_Plan_Allows()
        {
            var replicas = await appHost.GatewayReplicasRoutingEchoAsync();
            var started = TimeProvider.System.GetTimestamp();

            var responses = await Task.WhenAll(Enumerable.Range(0, ThrottledRequests)
                .Select(request => replicas[request % replicas.Count].GetAsync(RunningAppHost.EchoRequestPath)));

            var elapsed = TimeProvider.System.GetElapsedTime(started);
            var allowed = RunningAppHost.SeededBurst + (int)Math.Ceiling(elapsed.TotalSeconds * RunningAppHost.SeededRequestsPerSecond);
            Assert.Equal(2, replicas.Count);
            Assert.InRange(responses.Count(response => response.IsSuccessStatusCode), 0, allowed);
        }

        private Task<HttpClient> Gateway() => appHost.GatewayRoutingEchoAsync();

        private static DateTimeOffset UsageHour()
        {
            var now = TimeProvider.System.GetUtcNow();
            return new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, 0, 0, TimeSpan.Zero);
        }

        // Each call is traced, so the test waits until metering has counted it.
        private async Task CallEchoAndWaitForMeteringAsync(HttpClient gateway, int calls)
        {
            for (var i = 0; i < calls; i++)
            {
                var traceId = ActivityTraceId.CreateRandom().ToHexString();
                using var request = new HttpRequestMessage(HttpMethod.Get, RunningAppHost.EchoRequestPath);
                request.Headers.Add("traceparent", $"00-{traceId}-00f067aa0ba902b7-01");
                using var response = await gateway.SendAsync(request);
                await appHost.MeteredAsync(traceId);
            }
        }

        // Polls until the export and the recorded events agree, or the time is up.
        private async Task<(long Exported, long Recorded)> ExportedAndRecordedAsync(HttpClient gateway, DateTimeOffset hour, bool csv)
        {
            var deadline = TimeProvider.System.GetUtcNow() + RunningAppHost.MeteringTimeout;
            while (true)
            {
                var exported = await ExportedRequestsAsync(gateway, hour, csv);
                var recorded = await appHost.RecordedEchoRequestsAsync(hour, hour.AddHours(2));
                if (exported == recorded || TimeProvider.System.GetUtcNow() >= deadline)
                {
                    return (exported, recorded);
                }

                await Task.Delay(ExportPoll);
            }
        }

        // The seeded tenant's requests to Echo in the hour from hour on and the next.
        private static async Task<long> ExportedRequestsAsync(HttpClient gateway, DateTimeOffset hour, bool csv)
        {
            var path = $"/platform/v1/usage?from={hour:yyyy-MM-ddTHH:mm:ssZ}&to={hour.AddHours(2):yyyy-MM-ddTHH:mm:ssZ}&granularity=hour&module=echo&meter=requests";
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(csv ? UsageApi.CsvMediaType : "application/json"));
            using var response = await gateway.SendAsync(request);
            response.EnsureSuccessStatusCode();

            if (!csv)
            {
                var usage = await response.Content.ReadFromJsonAsync<UsageResponse>();
                return usage!.Usage.Sum(entry => entry.Quantity);
            }

            using var parser = new TextFieldParser(await response.Content.ReadAsStreamAsync());
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;
            var header = parser.ReadFields()!;
            var quantity = Array.IndexOf(header, "quantity");

            var total = 0L;
            while (!parser.EndOfData)
            {
                total += long.Parse(parser.ReadFields()![quantity], CultureInfo.InvariantCulture);
            }

            return total;
        }
    }
}
