// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Homeport.Platform.Metering.Api.Messaging;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Internal.Platform.Metering;

namespace Homeport.Platform.Metering.Api.Tests.Integration;

public sealed class ProgramTests
{
    public sealed class Main(RunningMetering metering) : IClassFixture<RunningMetering>
    {
        // The labels a homeport.* metric of metering may carry. Each is
        // bounded and never taken from an event: no tenant, trace id or route.
        // A new label is added here deliberately.
        [Fact]
        public async Task Labels_Its_Metrics_Only_With_Bounded_Values()
        {
            string[] allowed = [MeteringMetrics.OutcomeTag];
            using var labels = new MetricLabels();
            var message = new UsageRecorded(
                Guid.NewGuid(), DateTimeOffset.Parse("2026-11-09T10:00:00Z"), Guid.NewGuid(), "local-client", "echo", "/v1/echo", "requests", 1, 200, 12, "0af7651916cd43dd8448eb211c80319c");
            await metering.PublishAsync(message);

            await metering.PublishAsync(message);

            Assert.All(labels.Seen, seen => Assert.Contains(seen.Label, allowed));
        }

        [Fact]
        public async Task Serves_No_OpenApi_Document_Without_A_Token()
        {
            using var client = metering.CreateClient();

            using var response = await client.GetAsync(MeteringService.OpenApiPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Describes_The_Usage_Export_In_Its_OpenApi_Document()
        {
            var operation = await UsageOperationAsync();

            Assert.NotNull(operation);
        }

        [Fact]
        public async Task Describes_The_Query_Parameters_Of_The_Usage_Export()
        {
            var operation = await UsageOperationAsync();

            var names = operation!["parameters"]!.AsArray().Select(parameter => parameter!["name"]!.GetValue<string>());
            Assert.Equal(
                [UsageApi.FromParameter, UsageApi.ToParameter, UsageApi.GranularityParameter, UsageApi.ModuleParameter, UsageApi.MeterParameter],
                names);
        }

        [Fact]
        public async Task Describes_Json_And_Csv_As_Responses_Of_The_Usage_Export()
        {
            var operation = await UsageOperationAsync();

            var mediaTypes = operation!["responses"]!["200"]!["content"]!.AsObject().Select(entry => entry.Key);
            Assert.Equal([MediaTypeNames.Application.Json, UsageApi.CsvMediaType], mediaTypes);
        }

        private async Task<JsonObject?> UsageOperationAsync()
        {
            using var client = metering.CreateClient(metering.ServiceToken());

            var document = JsonNode.Parse(await client.GetStringAsync(MeteringService.OpenApiPath))!;

            return document["paths"]?[UsageApi.Path]?["get"]?.AsObject();
        }
    }
}
