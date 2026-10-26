// SPDX-License-Identifier: AGPL-3.0-or-later

using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Homeport.AppHost.Tests.Integration;

/// <summary>
/// The whole topology, started once for every test that shares it. Needs a
/// running Docker.
/// </summary>
public sealed class RunningAppHost : IAsyncLifetime
{
    /// <summary>
    /// Long enough for a first run that still has to pull every image.
    /// </summary>
    internal static readonly TimeSpan StartTimeout = TimeSpan.FromMinutes(5);

    private DistributedApplication? app;

    internal DistributedApplication App =>
        app ?? throw new InvalidOperationException("The AppHost has not been started.");

    public async Task InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(StartTimeout);

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Homeport_AppHost>(timeout.Token);
        app = await builder.BuildAsync(timeout.Token);
        await app.StartAsync(timeout.Token);
    }

    public async Task DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }
}
