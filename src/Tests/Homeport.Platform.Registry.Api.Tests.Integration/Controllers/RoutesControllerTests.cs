// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Shared.Client.Problems;
using Homeport.Shared.Internal.Platform.Registry;

namespace Homeport.Platform.Registry.Api.Tests.Integration.Controllers;

public sealed class RoutesControllerTests
{
    public sealed class Get(RunningRegistry registry) : IClassFixture<RunningRegistry>, IAsyncLifetime
    {
        public Task InitializeAsync() => registry.FlushAsync();

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task Answers_Unauthorized_Without_A_Service_Token()
        {
            var response = await registry.CreateClient().GetAsync(RegistryService.RoutesPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(new Uri(ProblemConventions.TypePrefix + "unauthorized"), problem?.Type);
        }

        [Fact]
        public async Task Answers_Unauthorized_For_A_Registration_Key()
        {
            var response = await registry.CreateModuleClient().GetAsync(RegistryService.RoutesPath);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Returns_The_Routes_Of_A_Registered_Instance()
        {
            await registry.CreateModuleClient().PutAsJsonAsync(InstancesControllerTests.InstancePath("routes-1"), InstancesControllerTests.Registration);

            var routes = await registry.CreateGatewayClient().GetFromJsonAsync<RouteTableResponse>(RegistryService.RoutesPath);

            var module = Assert.Single(routes!.Modules);
            Assert.Equal(RunningRegistry.ModuleId, module.ModuleId);
            var route = Assert.Single(module.Routes);
            Assert.Equal(("POST", "/v1/echo"), (route.Method, route.Path));
            Assert.Equal([InstancesControllerTests.Registration.Address], route.Instances);
        }

        [Fact]
        public async Task Answers_Not_Modified_While_The_Routes_Stay_The_Same()
        {
            var client = registry.CreateGatewayClient();
            var first = await client.GetAsync(RegistryService.RoutesPath);

            using var request = new HttpRequestMessage(HttpMethod.Get, RegistryService.RoutesPath);
            request.Headers.IfNoneMatch.Add(first.Headers.ETag!);
            var second = await client.SendAsync(request);

            Assert.Equal(HttpStatusCode.NotModified, second.StatusCode);
        }

        [Fact]
        public async Task Changes_The_ETag_When_An_Instance_Registers()
        {
            var client = registry.CreateGatewayClient();
            var before = await client.GetAsync(RegistryService.RoutesPath);
            await registry.CreateModuleClient().PutAsJsonAsync(InstancesControllerTests.InstancePath("routes-2"), InstancesControllerTests.Registration);

            var after = await client.GetAsync(RegistryService.RoutesPath);

            Assert.NotEqual(before.Headers.ETag, after.Headers.ETag);
        }
    }
}
