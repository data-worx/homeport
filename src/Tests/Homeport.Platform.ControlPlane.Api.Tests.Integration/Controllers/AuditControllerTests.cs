// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Homeport.Shared.Authentication;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.ControlPlane.Api.Tests.Integration.Controllers;

// Each test writes entries under an actor of its own and reads with that
// actor as a filter, so the tests share their control plane without seeing
// each other's entries.
public sealed class AuditControllerTests
{
    private const string InsertEntry =
        $"INSERT INTO {ControlPlaneDbContext.Schema}.\"{ControlPlaneDbContext.AuditEntriesTable}\" "
        + "(\"Id\", \"OccurredAt\", \"OperatorSubject\", \"OperatorName\", \"Action\", \"Target\", \"Summary\") "
        + "VALUES ($1, $2, $3, 'Audit Reader', 'tenant.renamed', $4, 'Renamed the tenant.')";

    private const string CountEntries =
        $"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"{ControlPlaneDbContext.AuditEntriesTable}\"";

    private static readonly DateTime Start = new(2026, 11, 16, 9, 0, 0, DateTimeKind.Utc);

    private static readonly Uri ValidationType = new(ProblemConventions.TypePrefix + "validation");

    private static readonly Uri InsufficientScopeType = new(ProblemConventions.TypePrefix + "insufficient-scope");

    public sealed class Get(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Entries_Of_The_Actor_Newest_First()
        {
            var actor = NewActor();
            var target = AuditEntry.TenantTarget(Guid.NewGuid());
            var oldest = await AddEntryAsync(actor, Start, target);
            var newest = await AddEntryAsync(actor, Start.AddMinutes(2), target);
            var middle = await AddEntryAsync(actor, Start.AddMinutes(1), target);

            var page = await ReadAsync($"?{AuditApi.ActorParameter}={actor}");

            Assert.Equal(
                [
                    new AuditEntryResponse(newest, Start.AddMinutes(2), actor, "Audit Reader", AuditEntry.TenantRenamed, target, "Renamed the tenant."),
                    new AuditEntryResponse(middle, Start.AddMinutes(1), actor, "Audit Reader", AuditEntry.TenantRenamed, target, "Renamed the tenant."),
                    new AuditEntryResponse(oldest, Start, actor, "Audit Reader", AuditEntry.TenantRenamed, target, "Renamed the tenant."),
                ],
                page.Entries);
        }

        [Fact]
        public async Task Returns_Only_The_Entries_About_The_Target()
        {
            var actor = NewActor();
            var target = AuditEntry.TenantTarget(Guid.NewGuid());
            var about = await AddEntryAsync(actor, Start, target);
            await AddEntryAsync(actor, Start, AuditEntry.TenantTarget(Guid.NewGuid()));

            var page = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.TargetParameter}={target}");

            Assert.Equal([about], page.Entries.Select(entry => entry.Id));
        }

        [Fact]
        public async Task Returns_Only_The_Entries_In_The_Period()
        {
            var actor = NewActor();
            await AddEntryAsync(actor, Start.AddMinutes(-1), NewTarget());
            var first = await AddEntryAsync(actor, Start, NewTarget());
            await AddEntryAsync(actor, Start.AddHours(1), NewTarget());

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
                ids.Add(await AddEntryAsync(actor, Start.AddMinutes(minute), NewTarget()));
            }

            var first = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=2");
            var second = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=2&{AuditApi.BeforeParameter}={first.Next}");
            var third = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=2&{AuditApi.BeforeParameter}={second.Next}");

            Assert.Equal(ids, first.Entries.Concat(second.Entries).Concat(third.Entries).Select(entry => entry.Id));
        }

        [Fact]
        public async Task Pages_Through_Entries_Of_The_Same_Moment_Without_Losing_One()
        {
            var actor = NewActor();
            var ids = new HashSet<Guid>();
            for (var i = 0; i < 3; i++)
            {
                ids.Add(await AddEntryAsync(actor, Start, NewTarget()));
            }

            var first = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=2");
            var second = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=2&{AuditApi.BeforeParameter}={first.Next}");

            Assert.Equal(ids, first.Entries.Concat(second.Entries).Select(entry => entry.Id).ToHashSet());
        }

        [Fact]
        public async Task Returns_No_Next_On_A_Page_That_Holds_The_Last_Entry()
        {
            var actor = NewActor();
            await AddEntryAsync(actor, Start, NewTarget());

            var page = await ReadAsync($"?{AuditApi.ActorParameter}={actor}&{AuditApi.LimitParameter}=1");

            Assert.Null(page.Next);
        }

        [Fact]
        public async Task Writes_No_Audit_Entry_For_The_Read()
        {
            var before = await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries);

            await ReadAsync(string.Empty);

            Assert.Equal(before, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries));
        }

        [Theory]
        [InlineData(AuditApi.LimitParameter, "0")]
        [InlineData(AuditApi.LimitParameter, "201")]
        [InlineData(AuditApi.BeforeParameter, "not-a-cursor")]
        [InlineData(AuditApi.ToParameter, "2026-11-16T09:00:00Z&from=2026-11-16T09:00:00Z")]
        public async Task Answers_A_Validation_Problem_For_An_Invalid_Parameter(string parameter, string value)
        {
            var response = await controlPlane.CreateOperatorClient().GetAsync($"{AuditApi.Path}?{parameter}={value}");

            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(
                (HttpStatusCode.BadRequest, ValidationType, true),
                (response.StatusCode, problem!.Type, problem.Errors!.ContainsKey(parameter)));
        }

        [Fact]
        public async Task Answers_Unauthorized_Without_A_Token()
        {
            var response = await controlPlane.CreateClient().GetAsync(AuditApi.Path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().GetAsync(AuditApi.Path);

            await AssertInsufficientScopeAsync(response);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Clients_Caller_Token_Even_With_The_Scope_Admin()
        {
            var response = await controlPlane.CreateCallerClient(GatewayTokens.AdminScope).GetAsync(AuditApi.Path);

            await AssertInsufficientScopeAsync(response);
        }

        private async Task<AuditEntriesResponse> ReadAsync(string query)
        {
            return (await controlPlane.CreateOperatorClient().GetFromJsonAsync<AuditEntriesResponse>(AuditApi.Path + query))!;
        }

        private async Task<Guid> AddEntryAsync(string actor, DateTime occurredAt, string target)
        {
            var id = Guid.NewGuid();
            await controlPlane.ExecuteScalarAsAppRoleAsync(InsertEntry, id, occurredAt, actor, target);
            return id;
        }

        private static string NewActor() => $"audit-reader-{Guid.NewGuid()}";

        private static string NewTarget() => AuditEntry.TenantTarget(Guid.NewGuid());

        private static async Task AssertInsufficientScopeAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(InsufficientScopeType, problem!.Type);
        }
    }
}
