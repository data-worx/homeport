// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Shared.Client.Platform.Registry.V1;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Registry.Api.Tests.Integration.Controllers;

public sealed class AdminModulesControllerTests
{
    public sealed class List(RunningRegistry registry) : IClassFixture<RunningRegistry>, IAsyncLifetime
    {
        public Task InitializeAsync() => registry.FlushAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Returns_A_Registered_Module_With_Its_Instance_And_Routes_To_The_Operator()
        {
            await registry.CreateModuleClient().PutAsJsonAsync(InstancesControllerTests.InstancePath("admin-1"), InstancesControllerTests.Registration);

            var modules = await registry.CreateOperatorClient().GetFromJsonAsync<ModulesResponse>(ModulesApi.Path);

            var module = Assert.Single(modules!.Modules);
            Assert.Equal(RunningRegistry.ModuleId, module.ModuleId);
            var instance = Assert.Single(module.Instances);
            Assert.Equal(
                ("admin-1", InstancesControllerTests.Registration.Address, InstancesControllerTests.Manifest.Version, InstancesControllerTests.Manifest.ContractVersion),
                (instance.InstanceId, instance.Address, instance.Version, instance.ContractVersion));
            Assert.True(instance.ExpiresAt > instance.LastHeartbeatAt);
            var route = Assert.Single(module.Routes);
            Assert.Equal(("POST", "/v1/echo", InstancesControllerTests.Manifest.Version), (route.Method, route.Path, route.Version));
        }

        [Fact]
        public async Task Leaves_Out_An_Instance_Whose_Registration_Expired()
        {
            var client = registry.CreateModuleClient();
            await client.PutAsJsonAsync(InstancesControllerTests.InstancePath("admin-live"), InstancesControllerTests.Registration);
            await client.PutAsJsonAsync(InstancesControllerTests.InstancePath("admin-expired"), InstancesControllerTests.Registration);
            await registry.ExpireAsync("admin-expired");

            var modules = await registry.CreateOperatorClient().GetFromJsonAsync<ModulesResponse>(ModulesApi.Path);

            Assert.Equal(["admin-live"], Assert.Single(modules!.Modules).Instances.Select(instance => instance.InstanceId));
        }

        [Fact]
        public async Task Answers_Forbidden_For_The_Gateways_Service_Token()
        {
            var response = await registry.CreateGatewayClient().GetAsync(ModulesApi.Path);

            await AssertForbiddenAsync(response);
        }

        [Fact]
        public async Task Answers_Forbidden_For_A_Token_With_A_Tenant()
        {
            var response = await registry.CreateTenantClient().GetAsync(ModulesApi.Path);

            await AssertForbiddenAsync(response);
        }

        [Fact]
        public async Task Answers_Unauthorized_Without_A_Token()
        {
            var response = await registry.CreateClient().GetAsync(ModulesApi.Path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Unauthorized_For_A_Registration_Key()
        {
            var response = await registry.CreateModuleClient().GetAsync(ModulesApi.Path);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        private static async Task AssertForbiddenAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(new Uri(ProblemConventions.TypePrefix + "insufficient-scope"), problem?.Type);
        }
    }
}
