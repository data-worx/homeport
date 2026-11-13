// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Platform.ControlPlane.Domain;
using Homeport.Platform.ControlPlane.Infrastructure.Persistence;
using Homeport.Shared.Authentication;
using Homeport.Shared.Client.Platform.ControlPlane.V1;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.ControlPlane.Api.Tests.Integration.Controllers;

// Each test creates tenants of its own, named after a fresh id, so the tests
// of a class share their control plane without seeing each other's changes.
public sealed class TenantsControllerTests
{
    private const string CountEntries =
        $"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"{ControlPlaneDbContext.AuditEntriesTable}\" WHERE \"Target\" = $1";

    private const string CountEntriesOf =
        $"SELECT count(*) FROM {ControlPlaneDbContext.Schema}.\"{ControlPlaneDbContext.AuditEntriesTable}\" "
        + "WHERE \"Target\" = $1 AND \"Action\" = $2 AND \"OperatorSubject\" = $3 AND \"OperatorName\" = $4";

    private static readonly Uri ValidationType = new(ProblemConventions.TypePrefix + "validation");

    private static readonly Uri InsufficientScopeType = new(ProblemConventions.TypePrefix + "insufficient-scope");

    public sealed class List(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Tenants_With_The_Number_Of_Their_Clients()
        {
            var response = await controlPlane.CreateOperatorClient().GetFromJsonAsync<TenantsResponse>(TenantsApi.Path);

            var known = RunningControlPlane.Tenant;
            Assert.Contains(new TenantResponse(known.Id, known.Name, 1), response!.Tenants);
        }

        [Fact]
        public async Task Answers_Unauthorized_Without_A_Token()
        {
            var response = await controlPlane.CreateClient().GetAsync(TenantsApi.Path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().GetAsync(TenantsApi.Path);

            await AssertInsufficientScopeAsync(response);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Clients_Caller_Token_Even_With_The_Scope_Admin()
        {
            var response = await controlPlane.CreateCallerClient(GatewayTokens.AdminScope).GetAsync(TenantsApi.Path);

            await AssertInsufficientScopeAsync(response);
        }
    }

    public sealed class Get(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Tenant()
        {
            var known = RunningControlPlane.Tenant;

            var tenant = await controlPlane.CreateOperatorClient().GetFromJsonAsync<TenantResponse>(PathOf(known.Id));

            Assert.Equal(new TenantResponse(known.Id, known.Name, 1), tenant);
        }

        [Fact]
        public async Task Answers_Not_Found_For_An_Unknown_Tenant()
        {
            var response = await controlPlane.CreateOperatorClient().GetAsync(PathOf(Guid.NewGuid()));

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().GetAsync(PathOf(RunningControlPlane.Tenant.Id));

            await AssertInsufficientScopeAsync(response);
        }
    }

    public sealed class Create(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Answers_Created_With_The_New_Tenant_And_Its_Address()
        {
            var name = NewName();

            var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(TenantsApi.Path, new CreateTenantRequest(name));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var tenant = await response.Content.ReadFromJsonAsync<TenantResponse>();
            Assert.Equal(name, tenant!.Name);
            Assert.Equal(0, tenant.ClientCount);
            Assert.EndsWith(PathOf(tenant.Id), response.Headers.Location!.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task Writes_One_Audit_Entry_Naming_The_Operator()
        {
            var tenant = await CreateAsync(controlPlane, NewName());

            var target = AuditEntry.TenantTarget(tenant.Id);
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, target));
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(
                CountEntriesOf, target, AuditEntry.TenantCreated, RunningControlPlane.OperatorSubject, RunningControlPlane.OperatorName));
        }

        [Fact]
        public async Task Accepts_A_Name_That_Differs_From_Another_Only_In_Case()
        {
            var name = NewName();
            await CreateAsync(controlPlane, name);

            var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(TenantsApi.Path, new CreateTenantRequest(name.ToUpperInvariant()));

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Name_For_A_Taken_Name()
        {
            var name = NewName();
            await CreateAsync(controlPlane, name);

            var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(TenantsApi.Path, new CreateTenantRequest(name));

            await AssertNameProblemAsync(response, $"A tenant named '{name}' already exists.");
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Name_Without_A_Name()
        {
            var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(TenantsApi.Path, new { });

            await AssertNameProblemAsync(response, "A tenant needs a name.");
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().PostAsJsonAsync(TenantsApi.Path, new CreateTenantRequest(NewName()));

            await AssertInsufficientScopeAsync(response);
        }
    }

    public sealed class Rename(RunningControlPlane controlPlane) : IClassFixture<RunningControlPlane>
    {
        [Fact]
        public async Task Returns_The_Renamed_Tenant()
        {
            var tenant = await CreateAsync(controlPlane, NewName());
            var name = NewName();

            var renamed = await RenameAsync(tenant.Id, name);

            Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);
            Assert.Equal(tenant with { Name = name }, await renamed.Content.ReadFromJsonAsync<TenantResponse>());
        }

        [Fact]
        public async Task Writes_One_Audit_Entry_Naming_The_Operator()
        {
            var tenant = await CreateAsync(controlPlane, NewName());

            await RenameAsync(tenant.Id, NewName());

            var target = AuditEntry.TenantTarget(tenant.Id);
            Assert.Equal(2L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, target));
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(
                CountEntriesOf, target, AuditEntry.TenantRenamed, RunningControlPlane.OperatorSubject, RunningControlPlane.OperatorName));
        }

        [Fact]
        public async Task Writes_No_Audit_Entry_When_The_Name_Stays_The_Same()
        {
            var tenant = await CreateAsync(controlPlane, NewName());

            await RenameAsync(tenant.Id, tenant.Name);

            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, AuditEntry.TenantTarget(tenant.Id)));
        }

        [Fact]
        public async Task Answers_A_Validation_Problem_At_The_Name_And_Writes_No_Audit_Entry_For_A_Taken_Name()
        {
            var taken = await CreateAsync(controlPlane, NewName());
            var tenant = await CreateAsync(controlPlane, NewName());

            var response = await RenameAsync(tenant.Id, taken.Name);

            await AssertNameProblemAsync(response, $"A tenant named '{taken.Name}' already exists.");
            Assert.Equal(1L, await controlPlane.ExecuteScalarAsAppRoleAsync(CountEntries, AuditEntry.TenantTarget(tenant.Id)));
        }

        [Fact]
        public async Task Answers_Not_Found_For_An_Unknown_Tenant()
        {
            var response = await RenameAsync(Guid.NewGuid(), NewName());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Forbidden_With_A_Service_Token()
        {
            var response = await controlPlane.CreateGatewayClient().PutAsJsonAsync(PathOf(RunningControlPlane.Tenant.Id), new RenameTenantRequest(NewName()));

            await AssertInsufficientScopeAsync(response);
        }

        private Task<HttpResponseMessage> RenameAsync(Guid tenantId, string name)
        {
            return controlPlane.CreateOperatorClient().PutAsJsonAsync(PathOf(tenantId), new RenameTenantRequest(name));
        }
    }

    private static string NewName() => $"Tenant {Guid.NewGuid()}";

    private static string PathOf(Guid tenantId)
    {
        return TenantsApi.TenantPath.Replace($"{{{TenantsApi.TenantIdRouteValue}}}", tenantId.ToString(), StringComparison.Ordinal);
    }

    private static async Task<TenantResponse> CreateAsync(RunningControlPlane controlPlane, string name)
    {
        var response = await controlPlane.CreateOperatorClient().PostAsJsonAsync(TenantsApi.Path, new CreateTenantRequest(name));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TenantResponse>())!;
    }

    private static async Task AssertNameProblemAsync(HttpResponseMessage response, string message)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal(ValidationType, problem!.Type);
        Assert.Equal([message], problem.Errors![TenantsApi.NameField]);
    }

    private static async Task AssertInsufficientScopeAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
        Assert.Equal(InsufficientScopeType, problem!.Type);
    }
}
