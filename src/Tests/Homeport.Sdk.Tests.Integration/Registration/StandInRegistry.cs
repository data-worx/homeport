// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Homeport.Sdk.Tests.Integration.Registration;

/// <summary>
/// A stand-in for the registry on a local port. It records every request and
/// answers registrations with <see cref="RegisterStatus"/> and heartbeats with
/// <see cref="HeartbeatStatus"/>; a refused registration carries
/// <see cref="RefusalBody"/>.
/// </summary>
internal sealed class StandInRegistry : IAsyncDisposable
{
    private const string LocalAddress = "http://127.0.0.1:0";

    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WaitPoll = TimeSpan.FromMilliseconds(20);

    private readonly ConcurrentQueue<Received> received = new();
    private WebApplication? app;

    internal int RegisterStatus { get; set; } = StatusCodes.Status204NoContent;

    internal int HeartbeatStatus { get; set; } = StatusCodes.Status204NoContent;

    internal string RefusalBody { get; set; } = "{}";

    internal Uri Url => new(app?.Urls.Single() ?? throw new InvalidOperationException("The stand-in has not been started."));

    internal IReadOnlyList<Received> Requests => [.. received];

    internal static async Task<StandInRegistry> StartAsync()
    {
        var registry = new StandInRegistry();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        registry.app = builder.Build();
        registry.app.Run(registry.AnswerAsync);
        await registry.app.StartAsync();

        return registry;
    }

    /// <summary>Waits until the stand-in received <paramref name="count"/> requests with <paramref name="method"/>.</summary>
    internal async Task<IReadOnlyList<Received>> WaitForAsync(string method, int count = 1)
    {
        using var timeout = new CancellationTokenSource(WaitTimeout);
        while (Matching(method).Count < count)
        {
            await Task.Delay(WaitPoll, timeout.Token);
        }

        return Matching(method);
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }

    private List<Received> Matching(string method) => Requests.Where(request => request.Method == method).ToList();

    private async Task AnswerAsync(HttpContext context)
    {
        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync();
        received.Enqueue(new Received(context.Request.Method, context.Request.Path, context.Request.Headers.Authorization.ToString(), body));

        if (HttpMethods.IsPut(context.Request.Method))
        {
            context.Response.StatusCode = RegisterStatus;
            if (RegisterStatus >= StatusCodes.Status400BadRequest)
            {
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(RefusalBody);
            }

            return;
        }

        context.Response.StatusCode = HeartbeatStatus;
    }

    internal sealed record Received(string Method, string Path, string Authorization, string Body);
}
