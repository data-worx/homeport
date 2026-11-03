// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

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

    /// <summary>The Keycloak client the control plane seeds, and the parameter that holds its secret.</summary>
    internal static readonly KeycloakClient SeededClient = new("local-client", "local-client-secret");

    /// <summary>A Keycloak client the control plane does not know.</summary>
    internal static readonly KeycloakClient UnmappedClient = new("unmapped-client", "unmapped-client-secret");

    private const string TokenPath = "/realms/homeport/protocol/openid-connect/token";

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
    /// A client of the gateway with a token of <see cref="SeededClient"/>, once
    /// Echo, the gateway, Keycloak and the control plane are healthy and the
    /// gateway routes to Echo.
    /// </summary>
    internal async Task<HttpClient> GatewayRoutingEchoAsync()
    {
        using var timeout = new CancellationTokenSource(StartTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("echo", timeout.Token);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("gateway", timeout.Token);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("keycloak", timeout.Token);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("control-plane", timeout.Token);

        var gateway = App.CreateHttpClient("gateway");
        gateway.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(SeededClient));
        await StatusOnceAsync(gateway, status => status != HttpStatusCode.NotFound, RouteTimeout);

        return gateway;
    }

    /// <summary>An access token of <paramref name="client"/> from Keycloak, by the client-credentials grant.</summary>
    internal async Task<string> TokenAsync(KeycloakClient client)
    {
        using var timeout = new CancellationTokenSource(StartTimeout);
        var secret = await App.Services.GetRequiredService<DistributedApplicationModel>().Resources
            .OfType<ParameterResource>()
            .Single(parameter => parameter.Name == client.SecretParameter)
            .GetValueAsync(timeout.Token);

        using var keycloak = App.CreateHttpClient("keycloak");
        using var request = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = client.ClientId,
            ["client_secret"] = secret ?? string.Empty,
        });
        using var response = await keycloak.PostAsync(TokenPath, request, timeout.Token);
        response.EnsureSuccessStatusCode();

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
        return body.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Keycloak answered without an access token.");
    }

    /// <summary>
    /// Requests <see cref="EchoRequestPath"/> until the gateway answers with a
    /// status <paramref name="expected"/> accepts, and returns that status, or
    /// the last one once <paramref name="within"/> has passed.
    /// </summary>
    private static async Task<HttpStatusCode> StatusOnceAsync(
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

    /// <summary>A client in the Keycloak realm.</summary>
    /// <param name="ClientId">The client id, as the token carries it in <c>azp</c>.</param>
    /// <param name="SecretParameter">The AppHost parameter that holds the client's secret.</param>
    internal sealed record KeycloakClient(string ClientId, string SecretParameter);

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
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }
}
