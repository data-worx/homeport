// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Admin;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Routing;

/// <summary>
/// The gateway in memory with a short forwarding timeout, in front of a
/// stand-in that never answers: on a module route of Echo at
/// <see cref="ModulePath"/>, and on an admin route at <see cref="AdminPath"/>.
/// The stand-in waits until the gateway gives up on the request, so whatever
/// the timing, the gateway's answer is its timeout.
/// </summary>
public sealed class TimingOutGateway : IAsyncLifetime
{
    /// <summary>The forwarding timeout, short so a test does not wait long for it.</summary>
    internal const string Timeout = "00:00:00.200";

    /// <summary>The port the admin API answers on.</summary>
    internal const int AdminPort = 8081;

    /// <summary>The module route at the gateway.</summary>
    internal const string ModulePath = "/modules/echo" + ServicePath;

    /// <summary>The admin route at the gateway.</summary>
    internal const string AdminPath = "/platform" + AdminServicePath;

    private const string LocalAddress = "http://127.0.0.1:0";
    private const string ServicePath = "/v1/silent";
    private const string AdminServicePath = "/v1/admin/silent";

    private static readonly string[] AdminRoles = [StandInIdentityProvider.AdminRole];

    private WebApplication? silent;
    private StandInRegistry? registry;
    private StandInIdentityProvider? identityProvider;
    private StandInControlPlane? controlPlane;
    private WebApplicationFactory<Program>? gateway;

    private WebApplicationFactory<Program> Started =>
        gateway ?? throw new InvalidOperationException("The gateway has not been started.");

    private StandInIdentityProvider IdentityProvider =>
        identityProvider ?? throw new InvalidOperationException("The identity provider has not been started.");

    /// <summary>A client of the gateway with a token of a client the control plane knows.</summary>
    internal HttpClient CreateClient() =>
        Started.CreateClient().WithToken(IdentityProvider.Issue(StandInControlPlane.KnownClientId));

    /// <summary>A client whose requests arrive on the admin port, with an operator's token.</summary>
    internal HttpClient CreateAdminClient()
    {
        var server = Started.Server;

        return new HttpClient(server.CreateHandler(context => context.Connection.LocalPort = AdminPort))
        {
            BaseAddress = server.BaseAddress,
        }
        .WithToken(IdentityProvider.IssueOperator(AdminRoles));
    }

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        silent = builder.Build();
        silent.MapFallback(WaitUntilAbortedAsync);
        await silent.StartAsync();

        var address = silent.Urls.Single();
        registry = await StandInRegistry.StartAsync(StandInRegistry.EchoTable(address, ServicePath));
        identityProvider = await StandInIdentityProvider.StartAsync();
        controlPlane = await StandInControlPlane.StartAsync();

        gateway = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host
                .WithUsage()
                .WithTokenBuckets()
                .WithSigningKey()
                .WithRegistry(registry)
                .WithClients(identityProvider, controlPlane)
                .WithAdminApi(
                    AdminPort,
                    identityProvider,
                    new AdminEndpoint(HttpMethods.Get, AdminServicePath, new AdminService("silent", "homeport-silent", new Uri(address))))
                .UseSetting("Gateway:Forwarding:Timeout", Timeout)
                .ConfigureLogging(logging => logging.AddFakeLogging()));
        await ForwardingGateway.WaitForRoutesAsync(gateway);
    }

    public async Task DisposeAsync()
    {
        if (gateway is not null)
        {
            await gateway.DisposeAsync();
        }

        if (registry is not null)
        {
            await registry.DisposeAsync();
        }

        if (identityProvider is not null)
        {
            await identityProvider.DisposeAsync();
        }

        if (controlPlane is not null)
        {
            await controlPlane.DisposeAsync();
        }

        if (silent is not null)
        {
            await silent.StopAsync();
            await silent.DisposeAsync();
        }
    }

    // Answers nothing; returns once the gateway has given up on the request.
    private static async Task WaitUntilAbortedAsync(HttpContext context)
    {
        var aborted = new TaskCompletionSource();
        await using var registration = context.RequestAborted.Register(() => aborted.TrySetResult());
        await aborted.Task;
    }
}
