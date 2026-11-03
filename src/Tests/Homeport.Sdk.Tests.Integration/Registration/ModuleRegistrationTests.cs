// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Homeport.Sdk.Registration;
using Homeport.Shared.Client.ModuleContract;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Homeport.Sdk.Tests.Integration.Registration;

public sealed class ModuleRegistrationTests
{
    /// <remarks>
    /// <para>
    /// The heartbeat runs on a <see cref="FakeTimeProvider"/> the tests
    /// advance, so a test of two heartbeats takes milliseconds rather than
    /// twenty seconds. Only <see cref="ModuleRegistration"/> gets that clock
    /// (<see cref="UseTestClockForRegistration"/>); the container keeps
    /// <see cref="TimeProvider.System"/>.
    /// </para>
    /// <para>
    /// The reason is the standard resilience handler that
    /// <c>Homeport.Shared.ServiceDefaults</c> adds to every HTTP client, the
    /// registry client included. It takes its clock from the container and
    /// measures attempt timeouts, retry delays and the wait for in-flight
    /// calls on disposal on it. On the test's clock, advancing to the next
    /// heartbeat timed out a call still in flight, and its retry then waited
    /// for a clock nobody advanced again; disposing the module waited the same
    /// way. The tests hung or failed now and then, depending on whether a
    /// response was still on its way (#111).
    /// </para>
    /// </remarks>
    public sealed class ExecuteAsync : IAsyncLifetime
    {
        private const string Section = "Homeport:Registry";

        private const string Key = "things-registration-key";

        private static readonly Uri InstanceAddress = new("http://things-1:8080");

        private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(10);

        private readonly FakeTimeProvider time = new();
        private readonly CollectingLoggerProvider logs = new();
        private StandInRegistry? registry;

        private StandInRegistry Registry => registry!;

        public async Task InitializeAsync()
        {
            registry = await StandInRegistry.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await Registry.DisposeAsync();
        }

        [Fact]
        public async Task Registers_Once_Started_With_Its_Key_Address_And_Manifest()
        {
            await using var module = await StartAsync();

            var registration = Assert.Single(await Registry.WaitForAsync(HttpMethods.Put));

            Assert.Matches("^/instances/things/[0-9a-f]{32}$", registration.Path);
            Assert.Equal($"Bearer {Key}", registration.Authorization);
            var request = JsonSerializer.Deserialize<RegisterInstanceRequest>(registration.Body, JsonSerializerOptions.Web);
            Assert.Equal(InstanceAddress, request?.Address);
            Assert.Equal(TestModule.Manifest.Routes, request?.Manifest.Routes);
            Assert.Equal(ModuleManifest.CurrentContractVersion, request?.Manifest.ContractVersion);
        }

        [Fact]
        public async Task Sends_A_Heartbeat_For_The_Same_Instance_Every_Ten_Seconds()
        {
            await using var module = await StartAsync();
            var registration = Assert.Single(await Registry.WaitForAsync(HttpMethods.Put));

            time.Advance(TimeSpan.FromSeconds(10));
            var first = await Registry.WaitForAsync(HttpMethods.Post);
            time.Advance(TimeSpan.FromSeconds(10));
            var heartbeats = await Registry.WaitForAsync(HttpMethods.Post, count: 2);

            Assert.Single(first);
            Assert.All(heartbeats, heartbeat => Assert.Equal($"{registration.Path}/heartbeat", heartbeat.Path));
        }

        [Fact]
        public async Task Registers_Again_When_The_Registry_No_Longer_Knows_The_Instance()
        {
            await using var module = await StartAsync();
            await Registry.WaitForAsync(HttpMethods.Put);
            Registry.HeartbeatStatus = StatusCodes.Status404NotFound;

            time.Advance(TimeSpan.FromSeconds(10));
            var registrations = await Registry.WaitForAsync(HttpMethods.Put, count: 2);

            Assert.Equal(registrations[0].Path, registrations[1].Path);
        }

        [Fact]
        public async Task Stops_And_Logs_Why_When_The_Registry_Refuses_The_Manifest()
        {
            Registry.RegisterStatus = StatusCodes.Status422UnprocessableEntity;
            Registry.RefusalBody = """
                {"type":"https://homeport.data-worx.de/problems/invalid-manifest","title":"The manifest breaks the module contract.","status":422,"traceId":"0","errors":{"contractVersion":["Contract version 99 is not supported; supported versions: 0."]}}
                """;

            try
            {
                await using var module = await StartAsync();

                await WaitForStopAsync(module.App);

                var error = Assert.Single(logs.Entries, entry => entry.Level == LogLevel.Error);
                Assert.Contains("contractVersion: Contract version 99 is not supported; supported versions: 0.", error.Message);
                Assert.Equal(1, Environment.ExitCode);
            }
            finally
            {
                // The module sets it for its process, which here is the test run.
                Environment.ExitCode = 0;
            }
        }

        [Fact]
        public async Task Stops_When_The_Registry_Refuses_The_Key()
        {
            Registry.RegisterStatus = StatusCodes.Status401Unauthorized;

            try
            {
                await using var module = await StartAsync();

                await WaitForStopAsync(module.App);

                Assert.Contains(logs.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("401", StringComparison.Ordinal));
            }
            finally
            {
                Environment.ExitCode = 0;
            }
        }

        [Fact]
        public async Task Shuts_Down_While_A_Registration_Is_Unanswered()
        {
            var answer = new TaskCompletionSource();
            Registry.RegisterHeld = answer.Task;
            var module = await StartAsync();
            await Registry.HeldRegistrationReceived.WaitAsync(StopTimeout);

            try
            {
                await module.DisposeAsync().AsTask().WaitAsync(StopTimeout);
            }
            finally
            {
                answer.SetResult();
            }
        }

        [Fact]
        public async Task Does_Not_Register_And_Warns_Without_A_Registry_Url()
        {
            await using var app = await TestModule.StartAsync(logs);

            var warning = await logs.WaitForAsync(entry => entry.Level == LogLevel.Warning);

            Assert.Contains("does not register", warning.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Refuses_To_Start_With_A_Registry_Url_But_No_Key()
        {
            var start = TestModule.StartAsync(configure: builder =>
                builder.Configuration[$"{Section}:Url"] = Registry.Url.ToString());

            var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => start);

            Assert.Contains("Homeport:Registry:Key is not set.", exception.Message);
        }

        private static async Task WaitForStopAsync(WebApplication app)
        {
            var stopped = new TaskCompletionSource();
            using var onStopping = app.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.Register(stopped.SetResult);

            await stopped.Task.WaitAsync(StopTimeout);
        }

        private async Task<RunningModule> StartAsync()
        {
            return new RunningModule(await TestModule.StartAsync(logs, builder =>
            {
                builder.Configuration[$"{Section}:Url"] = Registry.Url.ToString();
                builder.Configuration[$"{Section}:Key"] = Key;
                builder.Configuration[$"{Section}:InstanceAddress"] = InstanceAddress.ToString();
                UseTestClockForRegistration(builder.Services);
            }));
        }

        // Gives the test's clock to the registration alone, in the place the SDK
        // registered it. See the remarks on this class for why not to the container.
        private void UseTestClockForRegistration(IServiceCollection services)
        {
            var registration = services.Single(service => service.ImplementationType == typeof(ModuleRegistration));
            services[services.IndexOf(registration)] = ServiceDescriptor.Singleton<IHostedService>(
                provider => ActivatorUtilities.CreateInstance<ModuleRegistration>(provider, time));
        }

        // Stops the module before disposing it, as the host does in production.
        // Disposed while running, the module disposes the resilience pipeline of
        // its registry client with a call still in flight, and the pipeline waits
        // up to 30 s for that call to finish
        // (Shuts_Down_While_A_Registration_Is_Unanswered).
        private sealed class RunningModule(WebApplication app) : IAsyncDisposable
        {
            internal WebApplication App => app;

            public async ValueTask DisposeAsync()
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
    }
}
