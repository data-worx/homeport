// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Client.Problems;
using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.VisualBasic.FileIO;

namespace Homeport.Platform.Metering.Api.Tests.Integration.Controllers;

public sealed class UsageControllerTests
{
    // Each test publishes for tenants of its own, so their usage never mixes.
    public sealed class Get(RunningMetering metering) : IClassFixture<RunningMetering>
    {
        private const string HourQuery = "/v1/usage?from=2026-11-04T13:00:00Z&to=2026-11-04T14:00:00Z&granularity=hour";

        private static readonly string[] ReadScope = [UsageApi.ReadScope];

        private static readonly DateTime Hour = new(2026, 11, 4, 13, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Returns_401_Without_A_Token()
        {
            using var client = metering.CreateClient();

            using var response = await client.GetAsync(HourQuery);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Returns_401_For_A_Token_Of_Another_Audience()
        {
            using var client = metering.CreateClient(metering.CallerToken(Guid.NewGuid(), ReadScope, audience: "homeport-registry"));

            using var response = await client.GetAsync(HourQuery);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Returns_403_For_A_Service_Token()
        {
            using var client = metering.CreateClient(metering.ServiceToken());

            using var response = await client.GetAsync(HourQuery);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Returns_403_Without_The_Scope()
        {
            using var client = metering.CreateClient(metering.CallerToken(Guid.NewGuid(), ["echo.invoke"]));

            using var response = await client.GetAsync(HourQuery);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Returns_The_Usage_Of_The_Hour_As_Json_By_Default()
        {
            var tenantId = await TenantWithRequestsAsync(3);
            using var client = metering.CreateClient(metering.CallerToken(tenantId, ReadScope));

            var usage = await client.GetFromJsonAsync<UsageResponse>(HourQuery);

            Assert.Equal(new UsageEntryResponse(Hour, "echo", "requests", 3), Assert.Single(usage!.Usage));
        }

        [Fact]
        public async Task Writes_Json_Timestamps_In_Utc_With_Z()
        {
            var tenantId = await TenantWithRequestsAsync(1);
            using var client = metering.CreateClient(metering.CallerToken(tenantId, ReadScope));

            var body = await client.GetStringAsync(HourQuery);

            Assert.Contains("\"period\":\"2026-11-04T13:00:00Z\"", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Returns_Csv_When_Asked_For_It()
        {
            var tenantId = await TenantWithRequestsAsync(1);
            using var client = metering.CreateClient(metering.CallerToken(tenantId, ReadScope));

            using var response = await client.SendAsync(CsvRequest());

            Assert.Equal(UsageApi.CsvMediaType, response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Returns_Csv_With_A_Header_Row_That_A_Csv_Parser_Reads()
        {
            var tenantId = await TenantWithRequestsAsync(3);
            using var client = metering.CreateClient(metering.CallerToken(tenantId, ReadScope));

            var rows = await CsvRowsAsync(client);

            Assert.Equal(
                [["period", "module", "meter", "quantity"], ["2026-11-04T13:00:00Z", "echo", "requests", "3"]],
                rows);
        }

        [Fact]
        public async Task Returns_Only_The_Json_Usage_Of_The_Callers_Tenant()
        {
            var tenantA = await TenantWithRequestsAsync(2);
            var tenantB = await TenantWithRequestsAsync(5);
            using var clientA = metering.CreateClient(metering.CallerToken(tenantA, ReadScope));
            using var clientB = metering.CreateClient(metering.CallerToken(tenantB, ReadScope));

            var usageA = await clientA.GetFromJsonAsync<UsageResponse>(HourQuery);
            var usageB = await clientB.GetFromJsonAsync<UsageResponse>(HourQuery);

            Assert.Equal((2L, 5L), (Assert.Single(usageA!.Usage).Quantity, Assert.Single(usageB!.Usage).Quantity));
        }

        [Fact]
        public async Task Returns_Only_The_Csv_Usage_Of_The_Callers_Tenant()
        {
            var tenantA = await TenantWithRequestsAsync(2);
            var tenantB = await TenantWithRequestsAsync(5);
            using var clientA = metering.CreateClient(metering.CallerToken(tenantA, ReadScope));
            using var clientB = metering.CreateClient(metering.CallerToken(tenantB, ReadScope));

            var rowsA = await CsvRowsAsync(clientA);
            var rowsB = await CsvRowsAsync(clientB);

            Assert.Equal((2, "2", 2, "5"), (rowsA.Count, rowsA[1][3], rowsB.Count, rowsB[1][3]));
        }

        [Fact]
        public async Task Returns_400_For_An_Unknown_Granularity()
        {
            using var client = metering.CreateClient(metering.CallerToken(Guid.NewGuid(), ReadScope));

            using var response = await client.GetAsync("/v1/usage?from=2026-11-04T13:00:00Z&to=2026-11-04T14:00:00Z&granularity=week");

            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(
                (HttpStatusCode.BadRequest, ProblemConventions.TypePrefix + "validation", true),
                (response.StatusCode, problem!.Type.ToString(), problem.Errors!.ContainsKey(UsageApi.GranularityParameter)));
        }

        [Fact]
        public async Task Returns_400_Without_From()
        {
            using var client = metering.CreateClient(metering.CallerToken(Guid.NewGuid(), ReadScope));

            using var response = await client.GetAsync("/v1/usage?to=2026-11-04T14:00:00Z&granularity=hour");

            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(
                (HttpStatusCode.BadRequest, true),
                (response.StatusCode, problem!.Errors!.ContainsKey(UsageApi.FromParameter)));
        }

        private static HttpRequestMessage CsvRequest()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, HourQuery);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(UsageApi.CsvMediaType));
            return request;
        }

        private static async Task<List<string[]>> CsvRowsAsync(HttpClient client)
        {
            using var response = await client.SendAsync(CsvRequest());
            using var parser = new TextFieldParser(await response.Content.ReadAsStreamAsync());
            parser.SetDelimiters(",");
            parser.HasFieldsEnclosedInQuotes = true;

            var rows = new List<string[]>();
            while (!parser.EndOfData)
            {
                rows.Add(parser.ReadFields()!);
            }

            return rows;
        }

        // A new tenant with this many requests to Echo in the hour of HourQuery.
        private async Task<Guid> TenantWithRequestsAsync(int requests)
        {
            var tenantId = Guid.NewGuid();
            for (var i = 0; i < requests; i++)
            {
                await metering.PublishAsync(new UsageRecorded(
                    Guid.NewGuid(),
                    DateTimeOffset.Parse("2026-11-04T13:30:00Z"),
                    tenantId,
                    "local-client",
                    "echo",
                    "/v1/echo",
                    "requests",
                    1,
                    200,
                    12,
                    "0af7651916cd43dd8448eb211c80319c"));
            }

            return tenantId;
        }
    }
}
