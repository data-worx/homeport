// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;
using Homeport.Shared.Internal.Platform.Registry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>
/// A stand-in for the registry on a local port. It serves <see cref="Table"/>
/// at <c>GET /routes</c> with an <c>ETag</c>, answers <c>304</c> for that
/// <c>ETag</c>, or <see cref="FailureStatus"/> when set, and records the
/// headers of every request once it has answered it.
/// </summary>
internal sealed class StandInRegistry : IAsyncDisposable
{
    private const string LocalAddress = "http://127.0.0.1:0";
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan WaitPoll = TimeSpan.FromMilliseconds(20);

    private readonly ConcurrentQueue<Received> received = new();
    private WebApplication? app;

    internal RouteTableResponse Table { get; set; } = new([]);

    /// <summary>When set, every request is answered with this status and no table.</summary>
    internal int? FailureStatus { get; set; }

    internal string Url => app?.Urls.Single() ?? throw new InvalidOperationException("The stand-in has not been started.");

    internal IReadOnlyList<Received> Requests => [.. received];

    /// <summary>Waits until the stand-in has answered <paramref name="count"/> requests.</summary>
    internal async Task<IReadOnlyList<Received>> WaitForAsync(int count = 1)
    {
        using var timeout = new CancellationTokenSource(WaitTimeout);
        while (received.Count < count)
        {
            await Task.Delay(WaitPoll, timeout.Token);
        }

        return Requests;
    }

    /// <summary>A table with Echo serving <paramref name="paths"/> on GET at <paramref name="address"/>.</summary>
    internal static RouteTableResponse EchoTable(string address, params string[] paths)
    {
        return new RouteTableResponse(
        [
            new ModuleRoutesResponse(
                "echo",
                paths.Select(path => new RouteResponse(HttpMethods.Get, path, "echo.invoke", "requests", 1, [new Uri(address)], "1.0.0", new Uri(new Uri(address), "/openapi/v1.json"))).ToList()),
        ]);
    }

    internal static async Task<StandInRegistry> StartAsync(RouteTableResponse table)
    {
        var registry = new StandInRegistry { Table = table };
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        registry.app = builder.Build();
        registry.app.MapGet(RegistryService.RoutesPath, registry.Answer);
        await registry.app.StartAsync();

        return registry;
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private IResult Answer(HttpContext context)
    {
        var request = new Received(context.Request.Headers.Authorization.ToString(), context.Request.Headers.IfNoneMatch.ToString());
        context.Response.OnCompleted(() =>
        {
            received.Enqueue(request);
            return Task.CompletedTask;
        });

        if (FailureStatus is { } status)
        {
            return Results.StatusCode(status);
        }

        var body = JsonSerializer.SerializeToUtf8Bytes(Table, JsonSerializerOptions.Web);
        var etag = $"\"{Convert.ToHexStringLower(SHA256.HashData(body))}\"";
        if (context.Request.Headers.IfNoneMatch == etag)
        {
            return Results.StatusCode(StatusCodes.Status304NotModified);
        }

        context.Response.Headers[HeaderNames.ETag] = etag;
        return Results.Bytes(body, "application/json");
    }

    internal sealed record Received(string Authorization, string IfNoneMatch);
}
