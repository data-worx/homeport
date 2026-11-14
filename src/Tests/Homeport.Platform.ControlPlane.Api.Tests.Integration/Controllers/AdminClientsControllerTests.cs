// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Homeport.Shared.Authentication;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.ControlPlane.Api.Tests.Integration.Controllers;

// Each test registers clients of its own, named after a fresh id, so the
// tests of a class share their control plane without seeing each other's
// changes.
public sealed class AdminClientsControllerTests
{
    private const string CountEntries =
        $"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"{ControlPlaneDbContext.AuditEntriesTable}\" WHERE \"Target\" = $1";

    private const string CountEntriesOf =
        $"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"{ControlPlaneDbContext.AuditEntriesTable}\" "
        + "WHERE \"Target\" = $1 AND \"Action\" = $2 AND \"OperatorSubject\" = $3 AND \"OperatorName\" = $4";

    private static readonly Uri ValidationType = new(ProblemConventions.TypePrefix + "validation");

    private static readonly Uri InsufficientScopeType = new(ProblemConventions.TypePrefix + "insufficient-scope");

    private static ClientResponse KnownClient => new(
        RunningControlPlane.ClientId,
        RunningControlPlane.Tenant.Id,
        RunningControlPlane.Tenant.Name,
        RunningControlPlane.Plan.Id,
        RunningControlPlane.Plan.Name);

    public sealed class List(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Clients_With_The_Names_Of_Their_Tenant_And_Plan()
        {
            var response = await controlPlane.CreateOperatorClient().GetFromJsonAsync<ClientsResponse>(ClientsApi.Path);

            Assert.Contains(KnownClient, response!.Clients);
        }

        [Fact]
        public async Task Returns_Only_The_Clients_Of_The_Tenant_Asked_For()
        {
            var tenant = await CreateTenantAsync(controlPlane);
            var client = await RegisterAsync(controlPlane, NewClientId(), tenant.Id, RunningControlPlane.Plan.Id);

            var response = await controlPlane.CreateOperatorClient().GetFromJsonAsync<ClientsResponse>($"{ClientsApi.Path}?{ClientsApi.TenantIdQuery}={tenant.Id}");

            Assert.Equal([client], response!.Clients);
        }

        [Fact]
        public async Task Answers_Unauthorized_Without_A_Token()
        {
            var response = await controlPlane.CreateClient().GetAsync(ClientsApi.Path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().GetAsync(ClientsApi.Path);

            await AssertInsufficientScopeAsync(response);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Clients_Caller_Token_Even_With_The_Scope_Admin()
        {
            var response = await controlPlane.CreateCallerClient(GatewayTokens.AdminScope).GetAsync(ClientsApi.Path);

            await AssertInsufficientScopeAsync(response);
        }
    }

    public sealed class Get(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Client()
        {
            var client = await controlPlane.CreateOperatorClient().GetFromJsonAsync<ClientResponse>(PathOf(RunningControlPlane.ClientId));

            Assert.Equal(KnownClient, client);
        }

        [Fact]
        public async Task Answers_Not_Found_For_A_Client_That_Is_Not_Registered()
        {
            var response = await controlPlane.CreateOperatorClient().GetAsync(PathOf(NewClientId()));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().GetAsync(PathOf(RunningControlPlane.ClientId));

            await AssertInsufficientScopeAsync(response);
        }
    }

    public sealed class Register(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Answers_Created_With_The_New_Client_And_Its_Address()
        {
            var clientId = NewClientId();
            var request = new RegisterClientRequest(clientId, RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(ClientsApi.Path, request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(KnownClient with { ClientId = clientId }, await response.Content.ReadFromJsonAsync<ClientResponse>());
            Assert.EndsWith(PathOf(clientId), response.Headers.Location!.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task Writes_One_Audit_Entry_Naming_The_Operator()
        {
            var client = await RegisterAsync(controlPlane, NewClientId(), RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            var target = AuditEntry.ClientTarget(client.ClientId);
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, target));
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(
                CountEntriesOf, target, AuditEntry.ClientRegistered, RunningControlPlane.OperatorSubject, RunningControlPlane.OperatorName));
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Client_Id_For_An_Empty_Client_Id()
        {
            var response = await PostAsync(string.Empty, RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            await AssertFieldProblemAsync(response, ClientsApi.ClientIdField, "A client needs the client id the identity provider issued.");
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Client_Id_And_Writes_No_Audit_Entry_For_A_Registered_Client()
        {
            var response = await PostAsync(RunningControlPlane.ClientId, RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            await AssertFieldProblemAsync(response, ClientsApi.ClientIdField, $"The client '{RunningControlPlane.ClientId}' is already registered.");
            Assert.Equal(0L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, AuditEntry.ClientTarget(RunningControlPlane.ClientId)));
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Tenant_For_An_Unknown_Tenant()
        {
            var response = await PostAsync(NewClientId(), Guid.NewGuid(), RunningControlPlane.Plan.Id);

            await AssertFieldProblemAsync(response, ClientsApi.TenantIdField, "There is no such tenant.");
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Plan_For_An_Unknown_Plan()
        {
            var response = await PostAsync(NewClientId(), RunningControlPlane.Tenant.Id, Guid.NewGuid());

            await AssertFieldProblemAsync(response, ClientsApi.PlanIdField, "There is no such plan.");
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var request = new RegisterClientRequest(NewClientId(), RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            var response = await controlPlane.CreateGatewayClient().PostAsJsonAsync(ClientsApi.Path, request);

            await AssertInsufficientScopeAsync(response);
        }

        private Task<HttpResponseMessage> PostAsync(string clientId, Guid tenantId, Guid planId)
        {
            return controlPlane.CreateOperatorClient().PostAsJsonAsync(ClientsApi.Path, new RegisterClientRequest(clientId, tenantId, planId));
        }
    }

    public sealed class ChangePlan(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Client_On_The_New_Plan()
        {
            var client = await RegisterAsync(controlPlane, NewClientId(), RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);
            var plan = await CreatePlanAsync(controlPlane);

            var response = await PutAsync(client.ClientId, plan.Id);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(client with { PlanId = plan.Id, PlanName = plan.Name }, await response.Content.ReadFromJsonAsync<ClientResponse>());
        }

        [Fact]
        public async Task Writes_One_Audit_Entry_Naming_The_Operator()
        {
            var client = await RegisterAsync(controlPlane, NewClientId(), RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);
            var plan = await CreatePlanAsync(controlPlane);

            await PutAsync(client.ClientId, plan.Id);

            var target = AuditEntry.ClientTarget(client.ClientId);
            Assert.Equal(2L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, target));
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(
                CountEntriesOf, target, AuditEntry.ClientPlanChanged, RunningControlPlane.OperatorSubject, RunningControlPlane.OperatorName));
        }

        [Fact]
        public async Task Writes_No_Audit_Entry_When_The_Plan_Is_Its_Own()
        {
            var client = await RegisterAsync(controlPlane, NewClientId(), RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            await PutAsync(client.ClientId, client.PlanId);

            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, AuditEntry.ClientTarget(client.ClientId)));
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Plan_And_Writes_No_Audit_Entry_For_An_Unknown_Plan()
        {
            var client = await RegisterAsync(controlPlane, NewClientId(), RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            var response = await PutAsync(client.ClientId, Guid.NewGuid());

            await AssertFieldProblemAsync(response, ClientsApi.PlanIdField, "There is no such plan.");
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, AuditEntry.ClientTarget(client.ClientId)));
        }

        [Fact]
        public async Task Answers_Not_Found_For_A_Client_That_Is_Not_Registered()
        {
            var response = await PutAsync(NewClientId(), RunningControlPlane.Plan.Id);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().PutAsJsonAsync(PathOf(RunningControlPlane.ClientId), new ChangeClientPlanRequest(RunningControlPlane.Plan.Id));

            await AssertInsufficientScopeAsync(response);
        }

        private Task<HttpResponseMessage> PutAsync(string clientId, Guid planId)
        {
            return controlPlane.CreateOperatorClient().PutAsJsonAsync(PathOf(clientId), new ChangeClientPlanRequest(planId));
        }
    }

    public sealed class Remove(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Answers_No_Content_And_Removes_The_Client()
        {
            var client = await RegisterAsync(controlPlane, NewClientId(), RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            var response = await controlPlane.CreateOperatorClient().DeleteAsync(PathOf(client.ClientId));

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await controlPlane.CreateOperatorClient().GetAsync(PathOf(client.ClientId))).StatusCode);
        }

        [Fact]
        public async Task Writes_One_Audit_Entry_Naming_The_Operator()
        {
            var client = await RegisterAsync(controlPlane, NewClientId(), RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            await controlPlane.CreateOperatorClient().DeleteAsync(PathOf(client.ClientId));

            var target = AuditEntry.ClientTarget(client.ClientId);
            Assert.Equal(2L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, target));
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(
                CountEntriesOf, target, AuditEntry.ClientRemoved, RunningControlPlane.OperatorSubject, RunningControlPlane.OperatorName));
        }

        [Fact]
        public async Task Answers_Not_Found_For_A_Client_That_Is_Not_Registered()
        {
            var response = await controlPlane.CreateOperatorClient().DeleteAsync(PathOf(NewClientId()));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var client = await RegisterAsync(controlPlane, NewClientId(), RunningControlPlane.Tenant.Id, RunningControlPlane.Plan.Id);

            var response = await controlPlane.CreateGatewayClient().DeleteAsync(PathOf(client.ClientId));

            await AssertInsufficientScopeAsync(response);
        }
    }

    private static string NewClientId() => $"client-{Guid.NewGuid()}";

    private static string PathOf(string clientId)
    {
        return ClientsApi.ClientPath.Replace($"{{{ClientsApi.ClientIdRouteValue}}}", Uri.EscapeDataString(clientId), StringComparison.Ordinal);
    }

    private static async Task<ClientResponse> RegisterAsync(RunningControlPlane controlPlane, string clientId, Guid tenantId, Guid planId)
    {
        var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(ClientsApi.Path, new RegisterClientRequest(clientId, tenantId, planId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ClientResponse>())!;
    }

    private static async Task<TenantResponse> CreateTenantAsync(RunningControlPlane controlPlane)
    {
        var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(TenantsApi.Path, new CreateTenantRequest($"Tenant {Guid.NewGuid()}"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TenantResponse>())!;
    }

    private static async Task<PlanResponse> CreatePlanAsync(RunningControlPlane controlPlane)
    {
        var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(PlansApi.Path, new CreatePlanRequest($"Plan {Guid.NewGuid()}", 10, 20));
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
