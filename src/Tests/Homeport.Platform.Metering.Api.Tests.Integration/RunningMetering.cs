// SPDX-License-Identifier: AGPL-3.0-or-later

using DotNet.Testcontainers.Configurations;
using Homeport.Platform.Metering.Api.Messaging;
using Homeport.Platform.Metering.Infrastructure.Persistence;
using Homeport.Shared.Internal.Platform.Metering;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.Tracking;

namespace Homeport.Platform.Metering.Api.Tests.Integration;

/// <summary>
/// Metering in memory, against a throwaway PostgreSQL prepared by
/// <c>deploy/postgres/init/init-homeport.sh</c> and migrated by
/// <c>migrate</c>, and a throwaway RabbitMQ, with a stand-in for the gateway
/// that publishes <see cref="UsageRecorded"/> the way the gateway does.
/// Needs a running Docker.
/// </summary>
public sealed class RunningMetering : IAsyncLifetime
{
    private const string Database = "homeport";

    private const string OwnerRole = "metering_owner";

    private const string AppRole = "metering_app";

    private const string OwnerPassword = "owner-password";

    private const string AppPassword = "app-password";

    private const string InitScript = "init-homeport.sh";

    private const string InitDirectory = "/docker-entrypoint-initdb.d/";

    private const string TenantSetting = "homeport.tenant_id";

    private const UnixFileModes Executable =
        UnixFileModes.UserRead | UnixFileModes.UserExecute
        | UnixFileModes.GroupRead | UnixFileModes.GroupExecute
        | UnixFileModes.OtherRead | UnixFileModes.OtherExecute;

    // Pinned like the AppHost pins them.
    private const string PostgresImage = "postgres:18.3@sha256:7e32e9833a6fb1c92c32552794cb6ed569d51b445a54907d35fc112ef39684db";

    private const string RabbitMqImage = "rabbitmq:4.3@sha256:8144dac709541439da6106d7421079132171b7f0a48b7a7b1f4bd39b22abdb75";

    // Long enough for a cold start of the handler; a delivery takes milliseconds.
    private static readonly TimeSpan DeliveryTimeout = TimeSpan.FromSeconds(30);

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder(PostgresImage)
        .WithDatabase(Database)
        .WithEnvironment("METERING_OWNER_PASSWORD", OwnerPassword)
        .WithEnvironment("METERING_APP_PASSWORD", AppPassword)
        // The script prepares every service's schema; this fixture uses only metering's.
        .WithEnvironment("CONTROL_PLANE_OWNER_PASSWORD", OwnerPassword)
        .WithEnvironment("CONTROL_PLANE_APP_PASSWORD", AppPassword)
        .WithResourceMapping(new FileInfo(Path.Combine(AppContext.BaseDirectory, InitScript)), InitDirectory, fileMode: Executable)
        .Build();

    private readonly RabbitMqContainer rabbitMq = new RabbitMqBuilder(RabbitMqImage).Build();

    private WebApplicationFactory<Program>? metering;
    private IHost? gateway;

    private WebApplicationFactory<Program> Metering =>
        metering ?? throw new InvalidOperationException("Metering has not been started.");

    private IHost Gateway => gateway ?? throw new InvalidOperationException("The gateway stand-in has not been started.");

    /// <summary>Metering's services.</summary>
    internal IServiceProvider Services => Metering.Services;

    /// <summary>
    /// Publishes <paramref name="message"/> as the gateway does and returns once
    /// metering has handled it.
    /// </summary>
    internal async Task PublishAsync(UsageRecorded message)
    {
        await Gateway.TrackActivity()
            .AlsoTrack(Metering.Services)
            .IncludeExternalTransports()
            .Timeout(DeliveryTimeout)
            .PublishMessageAndWaitAsync(message);
    }

    /// <summary>
    /// The hourly and daily quantity of <paramref name="tenantId"/>, read as the
    /// app role with the tenant set, as the service reads it.
    /// </summary>
    internal async Task<(long Hourly, long Daily)> QuantitiesAsync(Guid tenantId)
    {
        await using var connection = new NpgsqlConnection(ConnectionString(AppRole, AppPassword));
        await connection.OpenAsync();
        await using (var set = new NpgsqlCommand($"SELECT set_config('{TenantSetting}', @tenant, false)", connection))
        {
            set.Parameters.AddWithValue("tenant", tenantId.ToString());
            await set.ExecuteNonQueryAsync();
        }

        await using var sum = new NpgsqlCommand(
            "SELECT (SELECT coalesce(sum(\"Quantity\"), 0) FROM metering.\"UsageHourly\"), (SELECT coalesce(sum(\"Quantity\"), 0) FROM metering.\"UsageDaily\")",
            connection);
        await using var reader = await sum.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt64(0), reader.GetInt64(1));
    }

    public async Task InitializeAsync()
    {
        await Task.WhenAll(postgres.StartAsync(), rabbitMq.StartAsync());

        if (await MigrateCommand.RunAsync([$"--ConnectionStrings:{MigrateCommand.OwnerConnectionName}={ConnectionString(OwnerRole, OwnerPassword)}"]) != 0)
        {
            throw new InvalidOperationException("migrate failed; its log says why.");
        }

        metering = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host
                .UseSetting($"ConnectionStrings:{MeteringPersistenceExtensions.AppConnectionName}", ConnectionString(AppRole, AppPassword))
                .UseSetting($"ConnectionStrings:{MeteringMessagingExtensions.ConnectionName}", rabbitMq.GetConnectionString()));

        // Starts metering, so its queue exists before anything is published.
        _ = metering.Services;

        var builder = Host.CreateApplicationBuilder();
        builder.UseWolverine(options =>
        {
            options.UseRabbitMq(new Uri(rabbitMq.GetConnectionString()))
                .AutoProvision()
                .DeclareExchange(UsageMessaging.Exchange, exchange => exchange.ExchangeType = ExchangeType.Fanout);
            options.PublishMessage<UsageRecorded>().ToRabbitExchange(UsageMessaging.Exchange);
        });
        gateway = builder.Build();
        await gateway.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (gateway is not null)
        {
            await gateway.StopAsync();
            gateway.Dispose();
        }

        if (metering is not null)
        {
            await metering.DisposeAsync();
        }

        await Task.WhenAll(postgres.DisposeAsync().AsTask(), rabbitMq.DisposeAsync().AsTask());
    }

    private string ConnectionString(string role, string password)
    {
        return new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
        {
            Username = role,
            Password = password,
        }.ConnectionString;
    }
}
