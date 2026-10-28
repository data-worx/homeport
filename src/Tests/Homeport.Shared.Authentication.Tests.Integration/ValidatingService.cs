// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Shared.Authentication.Tests.Integration;

/// <summary>
/// A service that accepts gateway tokens for <see cref="Audience"/>, in memory,
/// next to a stand-in for the gateway's JWKS on a local port. A test changes
/// what the JWKS publishes with <see cref="Publish"/>.
/// </summary>
internal sealed class ValidatingService : IAsyncDisposable
{
    internal const string Audience = "homeport-registry";

    /// <summary>The one endpoint of the service; it requires a valid token.</summary>
    internal const string ProtectedPath = "/protected";

    private const string LocalAddress = "http://127.0.0.1:0";

    private WebApplication? jwks;
    private WebApplication? service;
    private string document = TestKey.Jwks();

    internal HttpClient CreateClient() =>
        (service ?? throw new InvalidOperationException("The service has not been started.")).GetTestClient();

    internal static async Task<ValidatingService> StartAsync(params TestKey[] published)
    {
        var started = new ValidatingService();
        started.Publish(published);
        await started.StartJwksAsync();
        await started.StartServiceAsync();

        return started;
    }

    internal void Publish(params TestKey[] keys)
    {
        document = TestKey.Jwks(keys);
    }

    public async ValueTask DisposeAsync()
    {
        if (service is not null)
        {
            await service.DisposeAsync();
        }

        if (jwks is not null)
        {
            await jwks.DisposeAsync();
        }
    }

    private async Task StartJwksAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        jwks = builder.Build();
        jwks.Run(context => context.Response.WriteAsync(document));
        await jwks.StartAsync();
    }

    private async Task StartServiceAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration[$"{GatewayTokenOptions.SectionName}:{nameof(GatewayTokenOptions.JwksUri)}"] =
            $"{jwks!.Urls.Single()}{GatewayTokens.JwksPath}";
        builder.Services.AddGatewayTokenAuthentication(Audience);
        builder.Services.AddAuthorization();

        service = builder.Build();
        service.UseAuthentication();
        service.UseAuthorization();
        service.MapGet(ProtectedPath, () => Results.Ok()).RequireAuthorization();
        await service.StartAsync();
    }
}
