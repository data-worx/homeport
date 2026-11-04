// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Homeport.Modules.Echo.Api.Tests.Integration;

/// <summary>
/// Echo in memory, accepting the module tokens of a stand-in for the gateway,
/// as it accepts only the real gateway's tokens when deployed.
/// </summary>
public sealed class EchoBehindGateway : IAsyncLifetime
{
    internal const string Audience = "echo";

    internal const string InvokeScope = "echo.invoke";

    private StandInGateway? gateway;
    private WebApplicationFactory<Program>? echo;

    internal StandInGateway Gateway => gateway ?? throw new InvalidOperationException("The gateway has not been started.");

    private WebApplicationFactory<Program> Started => echo ?? throw new InvalidOperationException("Echo has not been started.");

    /// <summary>A client of Echo with a module token granting <paramref name="scopes"/>.</summary>
    internal HttpClient CreateClient(string scopes = InvokeScope)
    {
        var client = Started.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Gateway.Issue(Audience, scopes));

        return client;
    }

    /// <summary>A client of Echo without a token.</summary>
    internal HttpClient CreateAnonymousClient() => Started.CreateClient();

    public async Task InitializeAsync()
    {
        gateway = await StandInGateway.StartAsync();
        echo = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host.UseSetting("GatewayTokens:JwksUri", gateway.JwksUri));
    }

    public async Task DisposeAsync()
    {
        if (echo is not null)
        {
            await echo.DisposeAsync();
        }

        if (gateway is not null)
        {
            await gateway.DisposeAsync();
        }
    }
}
