// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using Homeport.Shared.Internal.Platform.Registry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Routing;

public sealed class RouteTablePollerTests
{
    public sealed class ExecuteAsync : IAsyncLifetime
    {
        private const string LocalAddress = "http://127.0.0.1:0";
        private const string EchoPath = "/modules/echo/v1/request";
        private const string PollerCategory = "Homeport.Platform.Gateway.Api.Routing.RouteTablePoller";

        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan WaitPoll = TimeSpan.FromMilliseconds(20);

        private readonly FakeTimeProvider time = new(TimeProvider.System.GetUtcNow());
        private WebApplication? module;
        private StandInRegistry? registry;
        private WebApplicationFactory<Program>? gateway;

        private StandInRegistry Registry => registry!;

        private WebApplicationFactory<Program> Gateway => gateway!;

        public async Task InitializeAsync()
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls(LocalAddress);
            module = builder.Build();
            module.MapFallback(() => Results.Ok());
            await module.StartAsync();

            registry = await StandInRegistry.StartAsync(StandInRegistry.EchoTable(module.Urls.Single(), ForwardingGateway.RequestPath));
            gateway = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host => host
                    .WithSigningKey()
                    .WithRegistry(registry)
                    .ConfigureLogging(logging => logging.AddFakeLogging())
                    .ConfigureTestServices(services => services.AddSingleton<TimeProvider>(time)));
            await ForwardingGateway.WaitForRoutesAsync(gateway.CreateClient(), EchoPath);
        }

        public async Task DisposeAsync()
        {
            await Gateway.DisposeAsync();
            await Registry.DisposeAsync();
            await module!.DisposeAsync();
        }

        [Fact]
        public void Reads_The_Routes_With_A_Service_Token_For_The_Registry()
        {
            var header = AuthenticationHeaderValue.Parse(Registry.Requests[0].Authorization);

            var token = new JsonWebToken(header.Parameter);

            Assert.Equal([RegistryService.Audience], token.Audiences);
        }

        [Fact]
        public async Task Forwards_A_Declared_Route()
        {
            var response = await Gateway.CreateClient().GetAsync(EchoPath);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Not_Found_For_A_Path_The_Module_Does_Not_Declare()
        {
            var response = await Gateway.CreateClient().GetAsync("/modules/echo/v1/undeclared");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Method_Not_Allowed_As_Problem_For_A_Method_The_Route_Does_Not_Declare()
        {
            var response = await Gateway.CreateClient().PostAsync(EchoPath, content: null);

            Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.ProblemJson, response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Sends_The_ETag_It_Read_With_The_Next_Poll()
        {
            var polls = Registry.Requests.Count;

            time.Advance(PollInterval);
            await WaitUntil(() => Registry.Requests.Count > polls);

            Assert.NotEmpty(Registry.Requests[polls].IfNoneMatch);
        }

        [Fact]
        public async Task Removes_A_Route_The_Registry_No_Longer_Lists()
        {
            Registry.Table = new RouteTableResponse([]);

            time.Advance(PollInterval);
            await WaitUntil(async () => (await Gateway.CreateClient().GetAsync(EchoPath)).StatusCode == HttpStatusCode.NotFound);

            Assert.Equal(HttpStatusCode.NotFound, (await Gateway.CreateClient().GetAsync(EchoPath)).StatusCode);
        }

        [Fact]
        public async Task Keeps_The_Last_Known_Routes_While_The_Registry_Fails()
        {
            Registry.FailureStatus = StatusCodes.Status401Unauthorized;

            time.Advance(PollInterval);
            await WaitUntil(() => Gateway.Services.GetFakeLogCollector().GetSnapshot()
                .Any(entry => entry.Category == PollerCategory && entry.Level == LogLevel.Warning));

            Assert.Equal(HttpStatusCode.OK, (await Gateway.CreateClient().GetAsync(EchoPath)).StatusCode);
        }

        private static Task WaitUntil(Func<bool> condition) => WaitUntil(() => Task.FromResult(condition()));

        private static async Task WaitUntil(Func<Task<bool>> condition)
        {
            using var timeout = new CancellationTokenSource(WaitTimeout);
            while (!await condition())
            {
                await Task.Delay(WaitPoll, timeout.Token);
            }
        }
    }
}
