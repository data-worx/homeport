// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Platform.Metering.Domain;
using Homeport.Shared.Client.Platform.Metering.V1;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Metering.Api.Tests.Integration.Controllers;

public sealed class AuditControllerTests
{
    // Each test writes entries under an actor of its own and reads with that
    // actor as a filter, so the tests share their metering without seeing
    // each other's entries.
    public sealed class Get(RunningMetering metering) : IClassFixture<RunningMetering>
    {
        private static readonly DateTime Start = new(2026, 11, 16, 9, 0, 0, DateTimeKind.Utc);

        private static readonly Uri ValidationType = new(ProblemConventions.TypePrefix + "validation");

        [Fact]
        public async Task Returns_The_Entries_Of_The_Actor_Newest_First()
        {
            var actor = NewActor();
            var oldest = await metering.AddAuditEntryAsync(actor, Start, AuditEntry.AllTenantsTarget);
            var newest = await metering.AddAuditEntryAsync(actor, Start.AddMinutes(2), AuditEntry.AllTenantsTarget);
            var middle = await metering.AddAuditEntryAsync(actor, Start.AddMinutes(1), AuditEntry.AllTenantsTarget);

            var page = await ReadAsync($"?{AuditApi.ActorParameter}={actor}");

            Assert.Equal(
                [
                    new AuditEntryResponse(newest, Start.AddMinutes(2), actor, "Olivia Operator", AuditEntry.UsageRead, AuditEntry.AllTenantsTarget, "Read the usage."),
                    new AuditEntryResponse(middle, Start.AddMinutes(1), actor, "Olivia Operator", AuditEntry.UsageRead, AuditEntry.AllTenantsTarget, "Read the usage."),
                    new AuditEntryResponse(oldest, Start, actor, "Olivia Operator", AuditEntry.UsageRead, AuditEntry.AllTenantsTarget, "Read the usage."),
                ],
                page.Entries);
        }

        [Fact]
        public async Task Returns_Only_The_Entries_About_The_Target()
        {
            var actor = NewActor();
            var target = AuditEntry.TenantTarget(Guid.NewGuid());
            var about = await metering.AddAuditEntryAsync(actor, Start, target);
            await metering.AddAuditEntryAsync(actor, Start, AuditEntry.AllTenantsTarget);

            var page = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.TargetParameter}={target}");

            Assert.Equal([about], page.Entries.Select(entry => entry.Id));
        }

        [Fact]
        public async Task Returns_Only_The_Entries_In_The_Period()
        {
            var actor = NewActor();
            await metering.AddAuditEntryAsync(actor, Start.AddMinutes(-1), AuditEntry.AllTenantsTarget);
            var first = await metering.AddAuditEntryAsync(actor, Start, AuditEntry.AllTenantsTarget);
            await metering.AddAuditEntryAsync(actor, Start.AddHours(1), AuditEntry.AllTenantsTarget);

            var page = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.FromParameter}=2026-11-16T09:00:00Z&{AuditApi.ToParameter}=2026-11-16T10:00:00Z");

            Assert.Equal([first], page.Entries.Select(entry => entry.Id));
        }

        [Fact]
        public async Task Pages_Through_The_Entries_With_Next()
        {
            var actor = NewActor();
            var ids = new List<Guid>();
            for (var minute = 4; minute >= 0; minute--)
            {
                ids.Add(await metering.AddAuditEntryAsync(actor, Start.AddMinutes(minute), AuditEntry.AllTenantsTarget));
            }

            var first = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=2");
            var second = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=2&{AuditApi.BeforeParameter}={first.Next}");
            var third = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=2&{AuditApi.BeforeParameter}={second.Next}");

            Assert.Equal(ids, first.Entries.Concat(second.Entries).Concat(third.Entries).Select(entry => entry.Id));
        }

        [Fact]
        public async Task Returns_No_Next_On_A_Page_That_Holds_The_Last_Entry()
        {
            var actor = NewActor();
            await metering.AddAuditEntryAsync(actor, Start, AuditEntry.AllTenantsTarget);

            var page = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=1");

            Assert.Null(page.Next);
        }

        [Fact]
        public async Task Writes_No_Audit_Entry_For_The_Read()
        {
            var reader = NewActor();
            using var client = metering.CreateClient(metering.OperatorToken(reader));

            await client.GetFromJsonAsync<AuditEntriesResponse>(AuditApi.Path);

            Assert.Equal(0L, await metering.AuditEntriesOfAsync(reader));
        }

        [Theory]
        [InlineData(AuditApi.LimitParameter, "0")]
        [InlineData(AuditApi.BeforeParameter, "not-a-cursor")]
        public async Task Answers_A_Validation_Problem_For_An_Invalid_Parameter(string parameter, string value)
        {
            using var client = metering.CreateClient(metering.OperatorToken(NewActor()));

            using var response = await client.GetAsync($"{AuditApi.Path}?{parameter}={value}");

            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(
                (HttpStatusCode.BadRequest, ValidationType, true),
                (response.StatusCode, problem!.Type, problem.Errors!.ContainsKey(parameter)));
        }

        [Fact]
        public async Task Returns_401_Without_A_Token()
        {
            using var client = metering.CreateClient();

            using var response = await client.GetAsync(AuditApi.Path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Returns_403_For_A_Service_Token()
        {
            using var client = metering.CreateClient(metering.ServiceToken());

            using var response = await client.GetAsync(AuditApi.Path);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task Returns_403_For_A_Tenants_Caller_Token_Without_The_Admin_Scope()
        {
            using var client = metering.CreateClient(metering.CallerToken(Guid.NewGuid(), [UsageApi.ReadScope]));

            using var response = await client.GetAsync(AuditApi.Path);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        private async Task<AuditEntriesResponse> ReadAsync(string query)
        {
            using var client = metering.CreateClient(metering.OperatorToken(NewActor()));
            return (await client.GetFromJsonAsync<AuditEntriesResponse>(AuditApi.Path + query))!;
        }

        private static string NewActor() => $"audit-reader-{Guid.NewGuid()}";
    }
}
