// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>
/// The gateway in memory, forwarding to a stand-in for Echo on a local port.
/// The stand-in answers every request with the path and the headers it
/// received, so a test sees what the gateway forwards.
/// </summary>
public sealed class ForwardingGateway : IAsyncLifetime
{
    private const string LocalAddress = "http://127.0.0.1:0";

    // The configuration key service discovery resolves the service "echo" from.
    private const string EchoEndpointKey = "services:echo:http:0";

    private WebApplication? module;
    private WebApplicationFactory<Program>? gateway;

    internal HttpClient CreateClient() =>
        (gateway ?? throw new InvalidOperationException("The gateway has not been started.")).CreateClient();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        module = builder.Build();
        module.Run(context => context.Response.WriteAsJsonAsync(new ForwardedRequest(
            context.Request.Path,
            context.Request.Headers.ToDictionary(header => header.Key.ToLowerInvariant(), header => header.Value.ToString()))));
        await module.StartAsync();

        gateway = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host.UseSetting(EchoEndpointKey, module.Urls.Single()));
    }

    public async Task DisposeAsync()
    {
        if (gateway is not null)
        {
            await gateway.DisposeAsync();
        }

        if (module is not null)
        {
            await module.DisposeAsync();
        }
    }
}
