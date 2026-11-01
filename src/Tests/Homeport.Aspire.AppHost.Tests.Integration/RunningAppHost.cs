// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace Homeport.Aspire.AppHost.Tests.Integration;

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

    /// <summary>A path Echo serves through the gateway once it is registered.</summary>
    internal const string EchoRequestPath = "/modules/echo/v1/request";

    /// <summary>
    /// How long a registered route may take to appear at the gateway: Echo
    /// registers once started, and the gateway polls the registry every 5 s.
    /// </summary>
    internal static readonly TimeSpan RouteTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan RoutePoll = TimeSpan.FromMilliseconds(250);

    // Locally, Aspire's proxy in front of a stopped project still accepts the
    // gateway's connection and never answers, so a forward hangs rather than
    // failing. A request that takes longer counts as not answered yet.
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(3);

    private DistributedApplication? app;

    internal DistributedApplication App =>
        app ?? throw new InvalidOperationException("The AppHost has not been started.");

    /// <summary>
    /// A client of the gateway, once Echo and the gateway are healthy and the
    /// gateway routes to Echo.
    /// </summary>
    internal async Task<HttpClient> GatewayRoutingEchoAsync()
    {
        using var timeout = new CancellationTokenSource(StartTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("echo", timeout.Token);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("gateway", timeout.Token);

        var gateway = App.CreateHttpClient("gateway");
        await StatusOnceAsync(gateway, status => status != HttpStatusCode.NotFound, RouteTimeout);

        return gateway;
    }

    /// <summary>
    /// Requests <see cref="EchoRequestPath"/> until the gateway answers with a
    /// status <paramref name="expected"/> accepts, and returns that status, or
    /// the last one once <paramref name="within"/> has passed.
    /// </summary>
    internal static async Task<HttpStatusCode> StatusOnceAsync(
        HttpClient gateway,
        Func<HttpStatusCode, bool> expected,
        TimeSpan within)
    {
        var deadline = TimeProvider.System.GetUtcNow() + within;
        var status = await StatusAsync(gateway);
        while (!(status is { } answered && expected(answered)) && TimeProvider.System.GetUtcNow() < deadline)
        {
            await Task.Delay(RoutePoll);
            status = await StatusAsync(gateway);
        }

        return status ?? HttpStatusCode.RequestTimeout;
    }

    private static async Task<HttpStatusCode?> StatusAsync(HttpClient gateway)
    {
        using var timeout = new CancellationTokenSource(RequestTimeout);
        try
        {
            return (await gateway.GetAsync(EchoRequestPath, timeout.Token)).StatusCode;
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested)
        {
            return null;
        }
    }

    public async Task InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(StartTimeout);

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Homeport_Aspire_AppHost>(timeout.Token);
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
