// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>
/// A stand-in for the control plane on a local port. It knows
/// <see cref="KnownClientId"/>, answers <c>404</c> for every other client, and
/// records the headers of every request once it has answered it.
/// </summary>
internal sealed class StandInControlPlane : IAsyncDisposable
{
    internal const string KnownClientId = "known-client";

    internal const string UnknownClientId = "unknown-client";

    private const string LocalAddress = "http://127.0.0.1:0";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WaitPoll = TimeSpan.FromMilliseconds(20);

    private static readonly ClientResponse KnownClient = new(
        KnownClientId,
        Guid.Parse("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61"),
        new PlanResponse(Guid.Parse("8a2e4c6d-1f3b-4a5c-9d7e-0b1c2d3e4f50"), 10, 20));

    private readonly ConcurrentQueue<string> authorizations = new();
    private WebApplication? app;

    internal string Url => app?.Urls.Single() ?? throw new InvalidOperationException("The stand-in has not been started.");

    /// <summary>The <c>Authorization</c> header of every request, in order.</summary>
    internal IReadOnlyList<string> Authorizations => [.. authorizations];

    /// <summary>Waits until the stand-in has answered <paramref name="count"/> requests.</summary>
    internal async Task<IReadOnlyList<string>> WaitForAsync(int count = 1)
    {
        using var timeout = new CancellationTokenSource(WaitTimeout);
        while (authorizations.Count < count)
        {
            await Task.Delay(WaitPoll, timeout.Token);
        }

        return Authorizations;
    }

    internal static async Task<StandInControlPlane> StartAsync()
    {
        var controlPlane = new StandInControlPlane();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        controlPlane.app = builder.Build();
        controlPlane.app.MapGet(ControlPlaneService.ClientPath, controlPlane.Answer);
        await controlPlane.app.StartAsync();

        return controlPlane;
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private IResult Answer(HttpContext context, string clientId)
    {
        var authorization = context.Request.Headers.Authorization.ToString();
        context.Response.OnCompleted(() =>
        {
            authorizations.Enqueue(authorization);
            return Task.CompletedTask;
        });

        return clientId == KnownClientId ? Results.Json(KnownClient) : Results.NotFound();
    }
}
