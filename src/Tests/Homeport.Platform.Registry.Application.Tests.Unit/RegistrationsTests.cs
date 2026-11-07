// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;
using Homeport.Platform.Registry.Domain.Routing;

namespace Homeport.Platform.Registry.Application.Tests.Unit;

public sealed class RegistrationsTests
{
    private const string ModuleId = "things";

    private static readonly Instance Valid = new(
        "instance-1",
        new Uri("http://things-1:8080"),
        new Manifest(ModuleId, "Things", "1.0.0", 1, [new ManifestRoute("GET", "/v1/things", "things.read", "requests", 1)], ["things.read"], ["requests"], "/openapi/v1.json"));

    private readonly InMemoryStore store = new();
    private readonly Registrations registrations;

    public RegistrationsTests()
    {
        registrations = new Registrations(store, new Keys(new Dictionary<string, string[]> { [ModuleId] = ["key"] }));
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
            context.store.Listed.Add(Valid);

            var routes = await context.registrations.RoutesAsync(CancellationToken.None);

            Assert.Equal([Valid.Address], Assert.Single(Assert.Single(routes).Routes).Instances);
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

        internal List<Instance> Listed { get; } = [];

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

        public Task<IReadOnlyList<Instance>> ListAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<Instance>>(Listed);
        }
    }
}
