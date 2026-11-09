// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Npgsql;

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

    /// <summary>The scope Echo's routes require, which the seeded client gets only on request.</summary>
    internal const string EchoInvokeScope = "echo.invoke";

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

    /// <summary>
    /// The rate limit of the seeded plan in this run: small, so the throttling
    /// tests reach it with a handful of requests, however slow the machine.
    /// </summary>
    internal const int SeededRequestsPerSecond = 1;

    /// <summary>How many requests the seeded plan's bucket holds in this run.</summary>
    internal const int SeededBurst = 5;

    private const string GatewayEndpoint = "http";

    // Every replica is told where to listen; the dashboard shows only the proxy in front of them.
    private const string ListenUrlsVariable = "ASPNETCORE_URLS";

    /// <summary>The tenant the control plane seeds, as the AppHost's parameter names it.</summary>
    internal static readonly Guid SeededTenantId = Guid.Parse("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61");

    /// <summary>How long metering may take to record an event: the gateway publishes it once it answered.</summary>
    internal static readonly TimeSpan MeteringTimeout = TimeSpan.FromSeconds(30);

    private static readonly TimeSpan MeteringPoll = TimeSpan.FromMilliseconds(250);

    private const string EchoRequestsSql = """
        SELECT coalesce(sum("Quantity"), 0)::bigint FROM metering."UsageEvents"
        WHERE "ModuleId" = 'echo' AND "Meter" = 'requests' AND "OccurredAt" >= @from AND "OccurredAt" < @to
        """;

    // In one statement, so the event and the aggregates come from the same
    // snapshot; metering writes them in one transaction.
    private const string UsageOfTraceSql = """
        SELECT e."Quantity",
               coalesce((SELECT h."Quantity" FROM metering."UsageHourly" h
                         WHERE h."TenantId" = e."TenantId" AND h."ModuleId" = e."ModuleId" AND h."Meter" = e."Meter"
                           AND h."Hour" = date_trunc('hour', e."OccurredAt", 'UTC')), 0),
               (SELECT sum(o."Quantity") FROM metering."UsageEvents" o
                WHERE o."TenantId" = e."TenantId" AND o."ModuleId" = e."ModuleId" AND o."Meter" = e."Meter"
                  AND date_trunc('hour', o."OccurredAt", 'UTC') = date_trunc('hour', e."OccurredAt", 'UTC')),
               coalesce((SELECT d."Quantity" FROM metering."UsageDaily" d
                         WHERE d."TenantId" = e."TenantId" AND d."ModuleId" = e."ModuleId" AND d."Meter" = e."Meter"
                           AND d."Day" = (e."OccurredAt" AT TIME ZONE 'UTC')::date), 0),
               (SELECT sum(o."Quantity") FROM metering."UsageEvents" o
                WHERE o."TenantId" = e."TenantId" AND o."ModuleId" = e."ModuleId" AND o."Meter" = e."Meter"
                  AND (o."OccurredAt" AT TIME ZONE 'UTC')::date = (e."OccurredAt" AT TIME ZONE 'UTC')::date)
        FROM metering."UsageEvents" e
        WHERE e."TraceId" = @trace
        """;

    private DistributedApplication? app;

    internal DistributedApplication App =>
        app ?? throw new InvalidOperationException("The AppHost has not been started.");

    /// <summary>
    /// A client of the gateway with a token of <see cref="SeededClient"/>
    /// granting <see cref="EchoInvokeScope"/>, once
    /// Echo, the gateway, Keycloak and the control plane are healthy and the
    /// gateway routes to Echo. It sends a request that exceeds the seeded
    /// plan's limit again once the gateway says a token is free.
    /// </summary>
    internal Task<HttpClient> GatewayRoutingEchoAsync()
    {
        return RoutingEchoAsync(new HttpClient(new RetryAfterHandler()) { BaseAddress = App.GetEndpoint("gateway", GatewayEndpoint) });
    }

    /// <summary>
    /// Like <see cref="GatewayRoutingEchoAsync"/>, but a request that exceeds
    /// the seeded plan's limit is answered as the gateway answered it.
    /// </summary>
    internal Task<HttpClient> ThrottledGatewayRoutingEchoAsync()
    {
        return RoutingEchoAsync(App.CreateHttpClient("gateway"));
    }

    /// <summary>
    /// A client per gateway replica, sent to the replica itself rather than
    /// through the proxy in front of them, each like
    /// <see cref="ThrottledGatewayRoutingEchoAsync"/>.
    /// </summary>
    internal async Task<IReadOnlyList<HttpClient>> GatewayReplicasRoutingEchoAsync()
    {
        using var timeout = new CancellationTokenSource(StartTimeout);
        var replicas = App.Services.GetRequiredService<DistributedApplicationModel>().Resources
            .Single(resource => resource.Name == "gateway")
            .Annotations.OfType<ReplicaAnnotation>()
            .Single().Replicas;

        var addresses = new Dictionary<string, Uri>(StringComparer.Ordinal);
        await foreach (var update in App.ResourceNotifications.WatchAsync(timeout.Token))
        {
            var listening = update.Snapshot.EnvironmentVariables
                .FirstOrDefault(variable => variable.Name == ListenUrlsVariable)?.Value;
            if (update.Resource.Name == "gateway" && update.Snapshot.HealthStatus == HealthStatus.Healthy && listening is not null)
            {
                addresses[update.ResourceId] = new Uri(listening);
            }

            if (addresses.Count == replicas)
            {
                break;
            }
        }

        GatewayReplicaIds = [.. addresses.Keys];
        var clients = new List<HttpClient>();
        foreach (var address in addresses.Values)
        {
            clients.Add(await RoutingEchoAsync(new HttpClient { BaseAddress = address }));
        }

        return clients;
    }

    // DIAGNOSTIC (#154): the replicas the last GatewayReplicasRoutingEchoAsync found.
    internal IReadOnlyList<string> GatewayReplicaIds { get; private set; } = [];

    // DIAGNOSTIC (#154): the last lines of every replica's log.
    internal async Task<string> GatewayLogsAsync()
    {
        const int LinesPerReplica = 1000;
        var logs = App.Services.GetRequiredService<ResourceLoggerService>();
        var lines = new List<string>();
        foreach (var id in GatewayReplicaIds)
        {
            var replica = new List<string>();
            await foreach (var batch in logs.GetAllAsync(id))
            {
                replica.AddRange(batch.Select(line => $"{id}: {System.Text.RegularExpressions.Regex.Replace(line.Content, @"\u001b\[[0-9;]*m", string.Empty)}"));
            }

            lines.AddRange(replica.TakeLast(LinesPerReplica));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private async Task<HttpClient> RoutingEchoAsync(HttpClient gateway)
    {
        using var timeout = new CancellationTokenSource(StartTimeout);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("echo", timeout.Token);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("gateway", timeout.Token);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("keycloak", timeout.Token);
        await App.ResourceNotifications.WaitForResourceHealthyAsync("control-plane", timeout.Token);

        gateway.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await TokenAsync(SeededClient, EchoInvokeScope));
        await StatusOnceAsync(gateway, status => status != HttpStatusCode.NotFound, RouteTimeout);

        return gateway;
    }

    /// <summary>
    /// An access token of <paramref name="client"/> from Keycloak, by the
    /// client-credentials grant, asking for <paramref name="scope"/> on top of
    /// the client's default scopes.
    /// </summary>
    internal async Task<string> TokenAsync(KeycloakClient client, string? scope = null)
    {
        using var timeout = new CancellationTokenSource(StartTimeout);
        var secret = await App.Services.GetRequiredService<DistributedApplicationModel>().Resources
            .OfType<ParameterResource>()
            .Single(parameter => parameter.Name == client.SecretParameter)
            .GetValueAsync(timeout.Token);

        using var keycloak = App.CreateHttpClient("keycloak");
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = client.ClientId,
            ["client_secret"] = secret ?? string.Empty,
        };
        if (scope is not null)
        {
            form["scope"] = scope;
        }

        using var request = new FormUrlEncodedContent(form);
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

    /// <summary>
    /// Waits until metering has recorded the request traced as
    /// <paramref name="traceId"/>, and returns its quantity with the hourly and
    /// daily aggregate of its bucket and the sum of every event in that bucket.
    /// Read as metering's app role for <see cref="SeededTenantId"/>.
    /// </summary>
    internal async Task<MeteredUsage> MeteredAsync(string traceId)
    {
        using var timeout = new CancellationTokenSource(MeteringTimeout);
        await using var connection = await MeteringConnectionAsync(timeout.Token);

        while (true)
        {
            await using (var query = new NpgsqlCommand(UsageOfTraceSql, connection))
            {
                query.Parameters.AddWithValue("trace", traceId);
                await using var reader = await query.ExecuteReaderAsync(timeout.Token);
                if (await reader.ReadAsync(timeout.Token))
                {
                    return new MeteredUsage(reader.GetInt32(0), reader.GetInt64(1), reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4));
                }
            }

            await Task.Delay(MeteringPoll, timeout.Token);
        }
    }

    /// <summary>
    /// The sum of the quantities of every event metering holds for
    /// <see cref="SeededTenantId"/> of Echo's meter <c>requests</c> in
    /// [<paramref name="from"/>, <paramref name="to"/>).
    /// </summary>
    internal async Task<long> RecordedEchoRequestsAsync(DateTimeOffset from, DateTimeOffset to)
    {
        using var timeout = new CancellationTokenSource(MeteringTimeout);
        await using var connection = await MeteringConnectionAsync(timeout.Token);
        await using var query = new NpgsqlCommand(EchoRequestsSql, connection);
        query.Parameters.AddWithValue("from", from);
        query.Parameters.AddWithValue("to", to);

        return (long)(await query.ExecuteScalarAsync(timeout.Token))!;
    }

    private async Task<NpgsqlConnection> MeteringConnectionAsync(CancellationToken cancellationToken)
    {
        var password = await App.Services.GetRequiredService<DistributedApplicationModel>().Resources
            .OfType<ParameterResource>()
            .Single(parameter => parameter.Name == "metering-app-password")
            .GetValueAsync(cancellationToken);
        var postgres = App.GetEndpoint("postgres", "tcp");

        var connection = new NpgsqlConnection(new NpgsqlConnectionStringBuilder
        {
            Host = postgres.Host,
            Port = postgres.Port,
            Database = "homeport",
            Username = "metering_app",
            Password = password,
        }.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var set = new NpgsqlCommand("SELECT set_config('homeport.tenant_id', @tenant, false)", connection);
        set.Parameters.AddWithValue("tenant", SeededTenantId.ToString());
        await set.ExecuteNonQueryAsync(cancellationToken);

        return connection;
    }

    /// <summary>What metering recorded for one request, and the aggregates of its bucket.</summary>
    /// <param name="Quantity">The quantity of the request's event.</param>
    /// <param name="Hourly">The hourly aggregate of its hour.</param>
    /// <param name="EventsInHour">The sum of the quantities of every event in that hour.</param>
    /// <param name="Daily">The daily aggregate of its day.</param>
    /// <param name="EventsOnDay">The sum of the quantities of every event on that day.</param>
    internal sealed record MeteredUsage(int Quantity, long Hourly, long EventsInHour, long Daily, long EventsOnDay);

    /// <summary>A client in the Keycloak realm.</summary>
    /// <param name="ClientId">The client id, as the token carries it in <c>azp</c>.</param>
    /// <param name="SecretParameter">The AppHost parameter that holds the client's secret.</param>
    internal sealed record KeycloakClient(string ClientId, string SecretParameter);

    public async Task InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(StartTimeout);

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.Homeport_Aspire_AppHost>(
            [
                $"Parameters:seed-plan-requests-per-second={SeededRequestsPerSecond}",
                $"Parameters:seed-plan-burst={SeededBurst}",
            ],
            timeout.Token);
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
