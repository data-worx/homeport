// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>
/// The gateway in memory, forwarding to a stand-in for Echo on a local port on
/// the routes a stand-in registry lists. The stand-in module answers every
/// request with the path and the headers it received, so a test sees what the
/// gateway forwards, except for the paths under <see cref="ErrorsPath"/>, which
/// answer with an error. Clients authenticate with a stand-in identity
/// provider and are looked up at a stand-in control plane. What the gateway
/// logs, down to Debug, is read from <see cref="Logs"/>, and the messages it
/// publishes are tracked through <see cref="Host"/>.
/// </summary>
public sealed class ForwardingGateway : IAsyncLifetime
{
    private const string LocalAddress = "http://127.0.0.1:0";

    /// <summary>The route the stand-in module answers with what it received.</summary>
    internal const string RequestPath = "/v1/request";

    /// <summary>The paths of the stand-in that answer with an error.</summary>
    internal const string ErrorsPath = "/v1/errors";

    /// <summary>A <c>404</c> as Problem Details of the stand-in's own type.</summary>
    internal const string NotFoundProblemPath = ErrorsPath + "/not-found-problem";

    /// <summary>A <c>500</c> as Problem Details whose detail carries internals.</summary>
    internal const string InternalProblemPath = ErrorsPath + "/internal-problem";

    /// <summary>A <c>400</c> with an HTML body.</summary>
    internal const string HtmlBadRequestPath = ErrorsPath + "/html-bad-request";

    internal const string NotFoundType = "https://stand-in.example/problems/not-found";

    internal const string NotFoundDetail = "No order 42.";

    internal const string InternalDetail = "Password=hunter2 rejected by the database.";

    // Logs at Information only when the routes changed.
    private const string PollerCategory = "Homeport.Platform.Gateway.Api.Routing.RouteTablePoller";

    private static readonly TimeSpan RoutesTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan RoutesPoll = TimeSpan.FromMilliseconds(50);

    private WebApplication? module;
    private StandInRegistry? registry;
    private StandInIdentityProvider? identityProvider;
    private StandInControlPlane? controlPlane;
    private WebApplicationFactory<Program>? gateway;

    /// <summary>A client of the gateway with a token of a client the control plane knows.</summary>
    internal HttpClient CreateClient() =>
        Started.CreateClient().WithToken(IdentityProvider.Issue(StandInControlPlane.KnownClientId));

    /// <summary>A client of the gateway without a token.</summary>
    internal HttpClient CreateAnonymousClient() =>
        Started.CreateClient();

    internal FakeLogCollector Logs => Started.Services.GetFakeLogCollector();

    /// <summary>The gateway's host, for tracking the messages it publishes.</summary>
    internal IHost Host => Started.Services.GetRequiredService<IHost>();

    internal StandInRegistry Registry => registry ?? throw new InvalidOperationException("The registry has not been started.");

    internal StandInIdentityProvider IdentityProvider => identityProvider ?? throw new InvalidOperationException("The identity provider has not been started.");

    internal StandInControlPlane ControlPlane => controlPlane ?? throw new InvalidOperationException("The control plane has not been started.");

    private WebApplicationFactory<Program> Started =>
        gateway ?? throw new InvalidOperationException("The gateway has not been started.");

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        module = builder.Build();
        module.MapGet(NotFoundProblemPath, () => Results.Problem(
            type: NotFoundType, detail: NotFoundDetail, statusCode: StatusCodes.Status404NotFound));
        module.MapGet(InternalProblemPath, () => Results.Problem(
            detail: InternalDetail, statusCode: StatusCodes.Status500InternalServerError));
        module.MapGet(HtmlBadRequestPath, () => Results.Content(
            "<html><body>Bad request</body></html>", MediaTypeNames.Text.Html, statusCode: StatusCodes.Status400BadRequest));
        module.MapFallback(context => context.Response.WriteAsJsonAsync(new ForwardedRequest(
            context.Request.Path,
            context.Request.Headers.ToDictionary(header => header.Key.ToLowerInvariant(), header => header.Value.ToString()))));
        await module.StartAsync();

        registry = await StandInRegistry.StartAsync(StandInRegistry.EchoTable(
            module.Urls.Single(), RequestPath, NotFoundProblemPath, InternalProblemPath, HtmlBadRequestPath));
        identityProvider = await StandInIdentityProvider.StartAsync();
        controlPlane = await StandInControlPlane.StartAsync();

        gateway = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host
                .WithUsage()
                .WithSigningKey()
                .WithRegistry(registry)
                .WithClients(identityProvider, controlPlane)
                .ConfigureLogging(logging => logging.AddFakeLogging().SetMinimumLevel(LogLevel.Debug)));
        await WaitForRoutesAsync(gateway);
    }

    /// <summary>
    /// Waits until <paramref name="gateway"/> has read its routes from the
    /// registry, which its poller logs once they are in place. A request cannot
    /// tell: without a token every module path is answered with <c>401</c>,
    /// routes or not, and with one the gateway asks the control plane, which a
    /// test may have made unreachable. The gateway logs through fake logging.
    /// </summary>
    internal static async Task WaitForRoutesAsync(WebApplicationFactory<Program> gateway)
    {
        var logs = gateway.Services.GetFakeLogCollector();
        using var timeout = new CancellationTokenSource(RoutesTimeout);
        while (!logs.GetSnapshot().Any(entry => entry.Category == PollerCategory && entry.Level == LogLevel.Information))
        {
            await Task.Delay(RoutesPoll, timeout.Token);
        }
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

        if (module is not null)
        {
            await module.StopAsync();
            await module.DisposeAsync();
        }
    }
}
