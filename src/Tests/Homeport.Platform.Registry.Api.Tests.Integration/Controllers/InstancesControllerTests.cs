// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using Homeport.Shared.Client.ModuleContract;
using Homeport.Shared.Client.Problems;

namespace Homeport.Platform.Registry.Api.Tests.Integration.Controllers;

public sealed class InstancesControllerTests
{
    internal static readonly ModuleManifest Manifest = new(
        Id: RunningRegistry.ModuleId,
        Name: "Echo",
        Version: "1.0.0",
        Routes: [new ManifestRoute("POST", "/v1/echo", "echo.invoke", "requests", 1)],
        Scopes: ["echo.invoke"],
        Meters: ["requests"]);

    internal static readonly RegisterInstanceRequest Registration = new(new Uri("http://echo-1:8080"), Manifest);

    internal static string InstancePath(string instanceId, string moduleId = RunningRegistry.ModuleId) =>
        RegistryPaths.Instance.Replace("{moduleId}", moduleId, StringComparison.Ordinal).Replace("{instanceId}", instanceId, StringComparison.Ordinal);

    internal static string HeartbeatPath(string instanceId) =>
        RegistryPaths.Heartbeat.Replace("{moduleId}", RunningRegistry.ModuleId, StringComparison.Ordinal).Replace("{instanceId}", instanceId, StringComparison.Ordinal);

    public sealed class Register(RunningRegistry registry) : IClassFixture<RunningRegistry>
    {
        [Fact]
        public async Task Registers_An_Instance_With_A_Valid_Key_And_Manifest()
        {
            var response = await registry.CreateModuleClient().PutAsJsonAsync(InstancePath("register-valid"), Registration);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Unauthorized_For_A_Wrong_Key()
        {
            var response = await registry.CreateModuleClient("wrong").PutAsJsonAsync(InstancePath("register-wrong-key"), Registration);

            await AssertUnauthorized(response);
        }

        [Fact]
        public async Task Answers_Unauthorized_Without_A_Key()
        {
            var response = await registry.CreateClient().PutAsJsonAsync(InstancePath("register-no-key"), Registration);

            await AssertUnauthorized(response);
        }

        [Fact]
        public async Task Answers_Unauthorized_For_A_Module_Id_It_Does_Not_Know()
        {
            var unknown = Registration with { Manifest = Manifest with { Id = "unknown" } };

            var response = await registry.CreateModuleClient().PutAsJsonAsync(InstancePath("register-unknown", "unknown"), unknown);

            await AssertUnauthorized(response);
        }

        [Fact]
        public async Task Answers_Unprocessable_For_An_Unsupported_Contract_Version_Naming_The_Supported_Ones()
        {
            var future = Registration with { Manifest = Manifest with { ContractVersion = 99 } };

            var response = await registry.CreateModuleClient().PutAsJsonAsync(InstancePath("register-future"), future);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.ProblemJson, response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(new Uri(ProblemConventions.TypePrefix + "invalid-manifest"), problem?.Type);
            Assert.Equal(["Contract version 99 is not supported; supported versions: 0."], problem?.Errors?["contractVersion"] ?? []);
        }

        [Fact]
        public async Task Answers_Unprocessable_For_A_Route_Path_Without_A_Major_Version()
        {
            var unversioned = Registration with { Manifest = Manifest with { Routes = [Manifest.Routes[0] with { Path = "/echo" }] } };

            var response = await registry.CreateModuleClient().PutAsJsonAsync(InstancePath("register-unversioned"), unversioned);

            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(["routes[0].path"], problem?.Errors?.Keys);
        }

        private static async Task AssertUnauthorized(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            var problem = await response.Content.ReadFromJsonAsync<ProblemResponse>();
            Assert.Equal(new Uri(ProblemConventions.TypePrefix + "unauthorized"), problem?.Type);
        }
    }

    public sealed class Heartbeat(RunningRegistry registry) : IClassFixture<RunningRegistry>
    {
        [Fact]
        public async Task Keeps_A_Registered_Instance()
        {
            var client = registry.CreateModuleClient();
            await client.PutAsJsonAsync(InstancePath("heartbeat-registered"), Registration);

            var response = await client.PostAsync(HeartbeatPath("heartbeat-registered"), content: null);

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Not_Found_For_An_Instance_It_Does_Not_Know()
        {
            var response = await registry.CreateModuleClient().PostAsync(HeartbeatPath("heartbeat-unknown"), content: null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.ProblemJson, response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Answers_Unauthorized_For_A_Wrong_Key()
        {
            var response = await registry.CreateModuleClient("wrong").PostAsync(HeartbeatPath("heartbeat-wrong-key"), content: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
