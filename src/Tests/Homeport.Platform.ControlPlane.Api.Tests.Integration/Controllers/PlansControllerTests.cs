// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Homeport.Shared.Authentication;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.ControlPlane.Api.Tests.Integration.Controllers;

// Each test creates plans of its own, named after a fresh id, so the tests
// of a class share their control plane without seeing each other's changes.
public sealed class PlansControllerTests
{
    private const string CountEntries =
        $"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"{ControlPlaneDbContext.AuditEntriesTable}\" WHERE \"Target\" = $1";

    private const string CountEntriesOf =
        $"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"{ControlPlaneDbContext.AuditEntriesTable}\" "
        + "WHERE \"Target\" = $1 AND \"Action\" = $2 AND \"OperatorSubject\" = $3 AND \"OperatorName\" = $4";

    private static readonly Uri ValidationType = new(ProblemConventions.TypePrefix + "validation");

    private static readonly Uri InsufficientScopeType = new(ProblemConventions.TypePrefix + "insufficient-scope");

    private static readonly Uri PlanInUseType = new(ProblemConventions.TypePrefix + "plan-in-use");

    public sealed class List(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Plans_With_Their_Limits_And_The_Number_Of_Their_Clients()
        {
            var response = await controlPlane.CreateOperatorClient().GetFromJsonAsync<PlansResponse>(PlansApi.Path);

            var known = RunningControlPlane.Plan;
            Assert.Contains(new PlanResponse(known.Id, known.Name, known.RequestsPerSecond, known.Burst, 1), response!.Plans);
        }

        [Fact]
        public async Task Answers_Unauthorized_Without_A_Token()
        {
            var response = await controlPlane.CreateClient().GetAsync(PlansApi.Path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().GetAsync(PlansApi.Path);

            await AssertInsufficientScopeAsync(response);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Clients_Caller_Token_Even_With_The_Scope_Admin()
        {
            var response = await controlPlane.CreateCallerClient(GatewayTokens.AdminScope).GetAsync(PlansApi.Path);

            await AssertInsufficientScopeAsync(response);
        }
    }

    public sealed class Get(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Plan()
        {
            var known = RunningControlPlane.Plan;

            var plan = await controlPlane.CreateOperatorClient().GetFromJsonAsync<PlanResponse>(PathOf(known.Id));

            Assert.Equal(new PlanResponse(known.Id, known.Name, known.RequestsPerSecond, known.Burst, 1), plan);
        }

        [Fact]
        public async Task Answers_Not_Found_For_An_Unknown_Plan()
        {
            var response = await controlPlane.CreateOperatorClient().GetAsync(PathOf(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().GetAsync(PathOf(RunningControlPlane.Plan.Id));

            await AssertInsufficientScopeAsync(response);
        }
    }

    public sealed class Create(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Answers_Created_With_The_New_Plan_And_Its_Address()
        {
            var name = NewName();

            var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(PlansApi.Path, new CreatePlanRequest(name, 50, 100));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var plan = await response.Content.ReadFromJsonAsync<PlanResponse>();
            Assert.Equal((name, 50, 100, 0), (plan!.Name, plan.RequestsPerSecond, plan.Burst, plan.ClientCount));
            Assert.EndsWith(PathOf(plan.Id), response.Headers.Location!.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task Writes_One_Audit_Entry_Naming_The_Operator()
        {
            var plan = await CreateAsync(controlPlane, NewName());

            var target = AuditEntry.PlanTarget(plan.Id);
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, target));
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(
                CountEntriesOf, target, AuditEntry.PlanCreated, RunningControlPlane.OperatorSubject, RunningControlPlane.OperatorName));
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Name_For_A_Taken_Name()
        {
            var name = NewName();
            await CreateAsync(controlPlane, name);

            var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(PlansApi.Path, new CreatePlanRequest(name, 10, 10));

            await AssertFieldProblemAsync(response, PlansApi.NameField, $"A plan named '{name}' already exists.");
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Rate_For_A_Rate_Of_Zero()
        {
            var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(PlansApi.Path, new CreatePlanRequest(NewName(), 0, 10));

            await AssertFieldProblemAsync(response, PlansApi.RequestsPerSecondField, "The requests per second must be at least 1.");
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Burst_For_A_Burst_Of_Zero()
        {
            var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(PlansApi.Path, new CreatePlanRequest(NewName(), 10, 0));

            await AssertFieldProblemAsync(response, PlansApi.BurstField, "The burst must be at least 1.");
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().PostAsJsonAsync(PlansApi.Path, new CreatePlanRequest(NewName(), 10, 10));

            await AssertInsufficientScopeAsync(response);
        }
    }

    public sealed class Change(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Changed_Plan()
        {
            var plan = await CreateAsync(controlPlane, NewName());
            var name = NewName();

            var changed = await ChangeAsync(plan.Id, name, 200, 400);

            Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
            Assert.Equal(plan with { Name = name, RequestsPerSecond = 200, Burst = 400 }, await changed.Content.ReadFromJsonAsync<PlanResponse>());
        }

        [Fact]
        public async Task Writes_One_Audit_Entry_Naming_The_Operator()
        {
            var plan = await CreateAsync(controlPlane, NewName());

            await ChangeAsync(plan.Id, plan.Name, 200, plan.Burst);

            var target = AuditEntry.PlanTarget(plan.Id);
            Assert.Equal(2L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, target));
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(
                CountEntriesOf, target, AuditEntry.PlanChanged, RunningControlPlane.OperatorSubject, RunningControlPlane.OperatorName));
        }

        [Fact]
        public async Task Writes_No_Audit_Entry_When_Nothing_Changes()
        {
            var plan = await CreateAsync(controlPlane, NewName());

            await ChangeAsync(plan.Id, plan.Name, plan.RequestsPerSecond, plan.Burst);

            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, AuditEntry.PlanTarget(plan.Id)));
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Name_And_Writes_No_Audit_Entry_For_A_Taken_Name()
        {
            var taken = await CreateAsync(controlPlane, NewName());
            var plan = await CreateAsync(controlPlane, NewName());

            var response = await ChangeAsync(plan.Id, taken.Name, plan.RequestsPerSecond, plan.Burst);

            await AssertFieldProblemAsync(response, PlansApi.NameField, $"A plan named '{taken.Name}' already exists.");
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, AuditEntry.PlanTarget(plan.Id)));
        }

        [Fact]
        public async Task Answers_Not_Found_For_An_Unknown_Plan()
        {
            var response = await ChangeAsync(Guid.NewGuid(), NewName(), 10, 10);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().PutAsJsonAsync(PathOf(RunningControlPlane.Plan.Id), new ChangePlanRequest(NewName(), 10, 10));

            await AssertInsufficientScopeAsync(response);
        }

        private Task<HttpResponseMessage> ChangeAsync(Guid planId, string name, int requestsPerSecond, int burst)
        {
            return controlPlane.CreateOperatorClient().PutAsJsonAsync(PathOf(planId), new ChangePlanRequest(name, requestsPerSecond, burst));
        }
    }

    public sealed class Delete(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Answers_No_Content_And_Deletes_The_Plan()
        {
            var plan = await CreateAsync(controlPlane, NewName());

            var response = await controlPlane.CreateOperatorClient().DeleteAsync(PathOf(plan.Id));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await controlPlane.CreateOperatorClient().GetAsync(PathOf(plan.Id))).StatusCode);
        }

        [Fact]
        public async Task Writes_One_Audit_Entry_Naming_The_Operator()
        {
            var plan = await CreateAsync(controlPlane, NewName());

            await controlPlane.CreateOperatorClient().DeleteAsync(PathOf(plan.Id));

            var target = AuditEntry.PlanTarget(plan.Id);
            Assert.Equal(2L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, target));
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(
                CountEntriesOf, target, AuditEntry.PlanDeleted, RunningControlPlane.OperatorSubject, RunningControlPlane.OperatorName));
        }

        [Fact]
        public async Task Answers_Plan_In_Use_And_Writes_No_Audit_Entry_For_A_Plan_A_Client_Uses()
        {
            var known = RunningControlPlane.Plan;

            var response = await controlPlane.CreateOperatorClient().DeleteAsync(PathOf(known.Id));

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(PlanInUseType, problem!.Type);
            Assert.Equal(0L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, AuditEntry.PlanTarget(known.Id)));
        }

        [Fact]
        public async Task Answers_Not_Found_For_An_Unknown_Plan()
        {
            var response = await controlPlane.CreateOperatorClient().DeleteAsync(PathOf(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var plan = await CreateAsync(controlPlane, NewName());

            var response = await controlPlane.CreateGatewayClient().DeleteAsync(PathOf(plan.Id));

            await AssertInsufficientScopeAsync(response);
        }
    }

    private static string NewName() => $"Plan {Guid.NewGuid()}";

    private static string PathOf(Guid planId)
    {
        return PlansApi.PlanPath.Replace($"{{{PlansApi.PlanIdRouteValue}}}", planId.ToString(), StringComparison.Ordinal);
    }

    private static async Task<PlanResponse> CreateAsync(RunningControlPlane controlPlane, string name)
    {
        var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(PlansApi.Path, new CreatePlanRequest(name, 10, 20));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PlanResponse>())!;
    }

    private static async Task AssertFieldProblemAsync(HttpResponseMessage response, string field, string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal(ValidationType, problem!.Type);
        Assert.Equal([message], problem.Errors![field]);
    }

    private static async Task AssertInsufficientScopeAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal(InsufficientScopeType, problem!.Type);
    }
}
