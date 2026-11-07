// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;
using Homeport.Platform.Registry.Domain.Routing;
using Homeport.Platform.Registry.Infrastructure.Instances;

namespace Homeport.Platform.Registry.Infrastructure.Tests.Integration.Instances;

public sealed class ValkeyInstanceStoreTests : IClassFixture<ValkeyContainer>, IAsyncLifetime
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan ShortLifetime = TimeSpan.FromMilliseconds(500);

    private static readonly Instance Echo = new(
        "instance-1",
        new Uri("http://echo-1:8080"),
        new Manifest("echo", "Echo", "1.0.0", 1, [new ManifestRoute("POST", "/v1/echo", "echo.invoke", "requests", 1)], ["echo.invoke"], ["requests"], "/openapi/v1.json"));

    private readonly ValkeyContainer valkey;
    private readonly ValkeyInstanceStore store;

    public ValkeyInstanceStoreTests(ValkeyContainer valkey)
    {
        this.valkey = valkey;
        store = new ValkeyInstanceStore(valkey.Connection);
    }

    public Task InitializeAsync() => valkey.FlushAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Lists_Nothing_Before_An_Instance_Registered()
    {
        var instances = await store.ListAsync(CancellationToken.None);

        Assert.Empty(instances);
    }

    [Fact]
    public async Task Lists_A_Saved_Instance_As_It_Was_Saved()
    {
        await store.SaveAsync(Echo, Lifetime, CancellationToken.None);

        var instances = await store.ListAsync(CancellationToken.None);

        var listed = Assert.Single(instances);
        Assert.Equal((Echo.InstanceId, Echo.Address), (listed.InstanceId, listed.Address));
        Assert.Equal(Echo.Manifest.Routes, listed.Manifest.Routes);
    }

    [Fact]
    public async Task Replaces_An_Instance_Saved_Again_Under_Its_Id()
    {
        await store.SaveAsync(Echo, Lifetime, CancellationToken.None);
        var moved = Echo with { Address = new Uri("http://echo-2:8080") };

        await store.SaveAsync(moved, Lifetime, CancellationToken.None);

        Assert.Equal(moved.Address, Assert.Single(await store.ListAsync(CancellationToken.None)).Address);
    }

    [Fact]
    public async Task Lets_Valkey_Expire_An_Instance_After_Its_Lifetime()
    {
        await store.SaveAsync(Echo, ShortLifetime, CancellationToken.None);

        var left = await valkey.TimeToLiveAsync();

        Assert.InRange(left.GetValueOrDefault(), TimeSpan.FromTicks(1), ShortLifetime);
    }

    [Fact]
    public async Task Extends_The_Lifetime_Of_A_Renewed_Instance()
    {
        await store.SaveAsync(Echo, ShortLifetime, CancellationToken.None);

        var renewed = await store.RenewAsync(Echo.Manifest.Id, Echo.InstanceId, Lifetime, CancellationToken.None);

        Assert.True(renewed);
        Assert.True(await valkey.TimeToLiveAsync() > ShortLifetime);
    }

    [Fact]
    public async Task Reports_A_Renewal_Of_An_Instance_It_Does_Not_Keep()
    {
        var renewed = await store.RenewAsync(Echo.Manifest.Id, Echo.InstanceId, Lifetime, CancellationToken.None);

        Assert.False(renewed);
    }
}
