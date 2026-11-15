// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;
using Homeport.Platform.Registry.Domain.Routing;
using Microsoft.Extensions.Time.Testing;

namespace Homeport.Platform.Registry.Application.Tests.Unit;

public sealed class RegistrationsTests
{
    private const string ModuleId = "things";

    private static readonly Instance Valid = new(
        "instance-1",
        new Uri("http://things-1:8080"),
        new Manifest(ModuleId, "Things", "1.0.0", 1, [new ManifestRoute("GET", "/v1/things", "things.read", "requests", 1)], ["things.read"], ["requests"], "/openapi/v1.json"));

    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly InMemoryStore store = new();
    private readonly Registrations registrations;

    public RegistrationsTests()
    {
        registrations = new Registrations(store, new Keys(new Dictionary<string, string[]> { [ModuleId] = ["key"] }), new FakeTimeProvider(Now));
    }

    public sealed class IsAuthorized
    {
        private readonly Registrations registrations = new RegistrationsTests().registrations;

        [Fact]
        public void Accepts_A_Configured_Key_For_Its_Module()
        {
            Assert.True(registrations.IsAuthorized(ModuleId, "key"));
        }

        [Fact]
        public void Rejects_A_Wrong_Key()
        {
            Assert.False(registrations.IsAuthorized(ModuleId, "other"));
        }

        [Fact]
        public void Rejects_A_Module_Id_Without_Keys()
        {
            Assert.False(registrations.IsAuthorized("unknown", "key"));
        }
    }

    public sealed class RegisterAsync
    {
        private readonly RegistrationsTests context = new();

        [Fact]
        public async Task Keeps_A_Valid_Instance_For_The_Instance_Lifetime()
        {
            var errors = await context.registrations.RegisterAsync(ModuleId, Valid, CancellationToken.None);

            Assert.Empty(errors);
            Assert.Equal((Valid, Registrations.InstanceLifetime), context.store.Saved.Single());
        }

        [Fact]
        public async Task Returns_The_Errors_Of_A_Broken_Manifest_And_Keeps_Nothing()
        {
            var broken = Valid with { Manifest = Valid.Manifest with { ContractVersion = 99 } };

            var errors = await context.registrations.RegisterAsync(ModuleId, broken, CancellationToken.None);

            Assert.Equal(["contractVersion"], errors.Keys);
            Assert.Empty(context.store.Saved);
        }
    }

    public sealed class RenewAsync
    {
        private readonly RegistrationsTests context = new();

        [Fact]
        public async Task Renews_A_Kept_Instance_For_The_Instance_Lifetime()
        {
            context.store.Kept.Add((ModuleId, Valid.InstanceId));

            var renewed = await context.registrations.RenewAsync(ModuleId, Valid.InstanceId, CancellationToken.None);

            Assert.True(renewed);
            Assert.Equal([Registrations.InstanceLifetime], context.store.Renewals);
        }

        [Fact]
        public async Task Reports_An_Instance_That_Is_No_Longer_Kept()
        {
            var renewed = await context.registrations.RenewAsync(ModuleId, Valid.InstanceId, CancellationToken.None);

            Assert.False(renewed);
        }
    }

    public sealed class RoutesAsync
    {
        private readonly RegistrationsTests context = new();

        [Fact]
        public async Task Builds_The_Routes_Of_The_Kept_Instances()
        {
            context.store.Listed.Add(new StoredInstance(Valid, Registrations.InstanceLifetime));

            var routes = await context.registrations.RoutesAsync(CancellationToken.None);

            Assert.Equal([Valid.Address], Assert.Single(Assert.Single(routes).Routes).Instances);
        }
    }

    public sealed class ModulesAsync
    {
        private readonly RegistrationsTests context = new();

        [Fact]
        public async Task Lists_Each_Module_Once_Ordered_By_Module_Id()
        {
            context.store.Listed.Add(new StoredInstance(Valid with { Manifest = Valid.Manifest with { Id = "zebra" } }, Registrations.InstanceLifetime));
            context.store.Listed.Add(new StoredInstance(Valid, Registrations.InstanceLifetime));
            context.store.Listed.Add(new StoredInstance(Valid with { InstanceId = "instance-2" }, Registrations.InstanceLifetime));

            var modules = await context.registrations.ModulesAsync(CancellationToken.None);

            Assert.Equal([ModuleId, "zebra"], modules.Select(module => module.ModuleId));
        }

        [Fact]
        public async Task Orders_The_Instances_Of_A_Module_By_Instance_Id()
        {
            context.store.Listed.Add(new StoredInstance(Valid with { InstanceId = "instance-2" }, Registrations.InstanceLifetime));
            context.store.Listed.Add(new StoredInstance(Valid, Registrations.InstanceLifetime));

            var modules = await context.registrations.ModulesAsync(CancellationToken.None);

            Assert.Equal(["instance-1", "instance-2"], Assert.Single(modules).Instances.Select(registered => registered.Instance.InstanceId));
        }

        [Fact]
        public async Task Dates_An_Instance_From_The_Time_It_Has_Left()
        {
            var left = TimeSpan.FromSeconds(20);
            context.store.Listed.Add(new StoredInstance(Valid, left));

            var modules = await context.registrations.ModulesAsync(CancellationToken.None);

            var registered = Assert.Single(Assert.Single(modules).Instances);
            Assert.Equal((Now + left - Registrations.InstanceLifetime, Now + left), (registered.LastHeartbeatAt, registered.ExpiresAt));
        }

        [Fact]
        public async Task Gives_A_Route_The_Version_Of_The_Newest_Instance_That_Declares_It()
        {
            context.store.Listed.Add(new StoredInstance(Valid, Registrations.InstanceLifetime));
            context.store.Listed.Add(new StoredInstance(Valid with { InstanceId = "instance-2", Manifest = Valid.Manifest with { Version = "1.1.0" } }, Registrations.InstanceLifetime));

            var modules = await context.registrations.ModulesAsync(CancellationToken.None);

            Assert.Equal("1.1.0", Assert.Single(Assert.Single(modules).Routes).Version);
        }
    }

    private sealed class Keys(Dictionary<string, string[]> keys) : IRegistrationKeySource
    {
        public IReadOnlyList<string> KeysFor(string moduleId) => keys.TryGetValue(moduleId, out var found) ? found : [];
    }

    private sealed class InMemoryStore : IInstanceStore
    {
        internal List<(Instance Instance, TimeSpan Lifetime)> Saved { get; } = [];

        internal List<(string ModuleId, string InstanceId)> Kept { get; } = [];

        internal List<TimeSpan> Renewals { get; } = [];

        internal List<StoredInstance> Listed { get; } = [];

        public Task SaveAsync(Instance instance, TimeSpan lifetime, CancellationToken cancellationToken)
        {
            Saved.Add((instance, lifetime));
            return Task.CompletedTask;
        }

        public Task<bool> RenewAsync(string moduleId, string instanceId, TimeSpan lifetime, CancellationToken cancellationToken)
        {
            Renewals.Add(lifetime);
            return Task.FromResult(Kept.Contains((moduleId, instanceId)));
        }

        public Task<IReadOnlyList<StoredInstance>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<StoredInstance>>(Listed);
        }
    }
}
