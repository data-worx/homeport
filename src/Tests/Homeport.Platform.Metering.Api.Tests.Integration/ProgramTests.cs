// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Mime;
using System.Text.Json.Nodes;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Internal.Platform.Metering;

namespace Homeport.Platform.Metering.Api.Tests.Integration;

public sealed class ProgramTests
{
    public sealed class Main(RunningMetering metering) : IClassFixture<RunningMetering>
    {
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
