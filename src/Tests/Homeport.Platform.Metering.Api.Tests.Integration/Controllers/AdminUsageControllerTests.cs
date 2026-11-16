// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Client.Problems;
using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.VisualBasic.FileIO;

namespace Homeport.Platform.Metering.Api.Tests.Integration.Controllers;

public sealed class AdminUsageControllerTests
{
    // Each test publishes for tenants of its own and reads as an operator of
    // its own, so neither usage nor audit entries mix between tests.
    public sealed class Get(RunningMetering metering) : IClassFixture<RunningMetering>
    {
        private const string HourQuery = "/v1/admin/usage?from=2026-11-04T13:00:00Z&to=2026-11-04T14:00:00Z&granularity=hour";

        private static readonly DateTime Hour = new(2026, 11, 4, 13, 0, 0, DateTimeKind.Utc);

        [Fact]
        public async Task Returns_401_Without_A_Token()
        {
            using var client = metering.CreateClient();

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
        public async Task Returns_403_For_A_Tenants_Caller_Token_Without_The_Admin_Scope()
        {
            using var client = metering.CreateClient(metering.CallerToken(Guid.NewGuid(), [UsageApi.ReadScope]));

            using var response = await client.GetAsync(HourQuery);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Returns_The_Rows_Of_Every_Tenant()
        {
            var tenantA = await TenantWithRequestsAsync(2);
            var tenantB = await TenantWithRequestsAsync(5);
            using var client = OperatorClient();

            var usage = await client.GetFromJsonAsync<AdminUsageResponse>(HourQuery);

            Assert.Equal(
                [new AdminUsageEntryResponse(Hour, tenantA, "echo", "requests", 2), new AdminUsageEntryResponse(Hour, tenantB, "echo", "requests", 5)],
                usage!.Usage.Where(entry => entry.Tenant == tenantA || entry.Tenant == tenantB).OrderBy(entry => entry.Quantity));
        }

        [Fact]
        public async Task Returns_Only_The_Rows_Of_The_Tenant_Asked_For()
        {
            var tenantA = await TenantWithRequestsAsync(2);
            await TenantWithRequestsAsync(5);
            using var client = OperatorClient();

            var usage = await client.GetFromJsonAsync<AdminUsageResponse>($"{HourQuery}&{AdminUsageApi.TenantParameter}={tenantA}");

            Assert.Equal(new AdminUsageEntryResponse(Hour, tenantA, "echo", "requests", 2), Assert.Single(usage!.Usage));
        }

        [Fact]
        public async Task Returns_Csv_With_The_Tenant_In_Every_Row()
        {
            var tenantId = await TenantWithRequestsAsync(3);
            using var client = OperatorClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{HourQuery}&{AdminUsageApi.TenantParameter}={tenantId}");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(UsageApi.CsvMediaType));

            using var response = await client.SendAsync(request);

            Assert.Equal(
                [["period", "tenant", "module", "meter", "quantity"], ["2026-11-04T13:00:00Z", tenantId.ToString(), "echo", "requests", "3"]],
                await CsvRowsAsync(response));
        }

        [Fact]
        public async Task Records_One_Audit_Entry_Per_Read()
        {
            var subject = $"operator-{Guid.NewGuid()}";
            using var client = metering.CreateClient(metering.OperatorToken(subject));

            await client.GetFromJsonAsync<AdminUsageResponse>(HourQuery);
            await client.GetFromJsonAsync<AdminUsageResponse>(HourQuery);

            Assert.Equal(2L, await metering.AuditEntriesOfAsync(subject));
        }

        [Fact]
        public async Task Records_No_Audit_Entry_For_A_Refused_Read()
        {
            var subject = $"operator-{Guid.NewGuid()}";
            using var client = metering.CreateClient(metering.OperatorToken(subject));

            using var response = await client.GetAsync("/v1/admin/usage?from=2026-11-04T13:00:00Z&to=2026-11-04T14:00:00Z&granularity=week");

            Assert.Equal((HttpStatusCode.BadRequest, 0L), (response.StatusCode, await metering.AuditEntriesOfAsync(subject)));
        }

        [Fact]
        public async Task Returns_400_For_An_Invalid_Tenant()
        {
            using var client = OperatorClient();

            using var response = await client.GetAsync($"{HourQuery}&{AdminUsageApi.TenantParameter}=not-a-tenant");

            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(
                (HttpStatusCode.BadRequest, ProblemConventions.TypePrefix + "validation", true),
                (response.StatusCode, problem!.Type.ToString(), problem.Errors!.ContainsKey(AdminUsageApi.TenantParameter)));
        }

        [Fact]
        public async Task Returns_400_And_Records_Nothing_For_A_Module_Too_Long_For_The_Audit_Entry()
        {
            var subject = $"operator-{Guid.NewGuid()}";
            using var client = metering.CreateClient(metering.OperatorToken(subject));

            using var response = await client.GetAsync($"{HourQuery}&{UsageApi.ModuleParameter}={new string('m', 900)}");

            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(
                (HttpStatusCode.BadRequest, true, 0L),
                (response.StatusCode, problem!.Errors!.ContainsKey(UsageApi.ModuleParameter), await metering.AuditEntriesOfAsync(subject)));
        }

        private HttpClient OperatorClient() => metering.CreateClient(metering.OperatorToken($"operator-{Guid.NewGuid()}"));

        private static async Task<List<string[]>> CsvRowsAsync(HttpResponseMessage response)
        {
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
