// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using DotNet.Testcontainers.Configurations;
using Homeport.Platform.ControlPlane.Persistence;
using Homeport.Platform.ControlPlane.Seeding;
using Homeport.Shared.Authentication;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Homeport.Platform.ControlPlane.Tests.Integration;

/// <summary>
/// The control plane in memory, against a throwaway PostgreSQL prepared by
/// <c>deploy/postgres/init/init-homeport.sh</c> and seeded by <c>migrate</c>,
/// and a stand-in for the gateway's JWKS on a local port. It knows the client
/// <see cref="ClientId"/> of <see cref="Tenant"/> on <see cref="Plan"/>.
/// Needs a running Docker.
/// </summary>
public sealed class RunningControlPlane : IAsyncLifetime
{
    internal const string ClientId = "known-client";

    private const string LocalAddress = "http://127.0.0.1:0";

    private const string Database = "homeport";

    private const string OwnerRole = "control_plane_owner";

    private const string AppRole = "control_plane_app";

    private const string OwnerPassword = "owner-password";

    private const string AppPassword = "app-password";

    private const string InitScript = "init-homeport.sh";

    private const string InitDirectory = "/docker-entrypoint-initdb.d/";

    private const UnixFileModes Executable =
        UnixFileModes.UserRead | UnixFileModes.UserExecute
        | UnixFileModes.GroupRead | UnixFileModes.GroupExecute
        | UnixFileModes.OtherRead | UnixFileModes.OtherExecute;

    // Pinned like the AppHost pins it.
    private const string PostgresImage = "postgres:18.3@sha256:7e32e9833a6fb1c92c32552794cb6ed569d51b445a54907d35fc112ef39684db";

    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder(PostgresImage)
        .WithDatabase(Database)
        .WithEnvironment("CONTROL_PLANE_OWNER_PASSWORD", OwnerPassword)
        .WithEnvironment("CONTROL_PLANE_APP_PASSWORD", AppPassword)
        .WithResourceMapping(new FileInfo(Path.Combine(AppContext.BaseDirectory, InitScript)), InitDirectory, fileMode: Executable)
        .Build();

    private readonly ECDsa gatewayKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private WebApplication? jwks;
    private WebApplicationFactory<Program>? controlPlane;

    internal static Tenant Tenant { get; } = new() { Id = Guid.Parse("6f1d2c5e-0b0a-4d6e-9a43-2a4f3b7c1e01"), Name = "Known tenant" };

    internal static Plan Plan { get; } = new() { Id = Guid.Parse("0c9e8b7a-6d5f-4e3c-8b2a-1f0e9d8c7b02"), Name = "Known plan", RequestsPerSecond = 10, Burst = 20 };

    private string AppConnectionString => ConnectionString(AppRole, AppPassword);

    private string OwnerConnectionString => ConnectionString(OwnerRole, OwnerPassword);

    private WebApplicationFactory<Program> Started =>
        controlPlane ?? throw new InvalidOperationException("The control plane has not been started.");

    /// <summary>A client without credentials.</summary>
    internal HttpClient CreateClient() => Started.CreateClient();

    /// <summary>A client that sends a gateway service token for the control plane.</summary>
    internal HttpClient CreateGatewayClient()
    {
        var client = Started.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, ServiceToken());
        return client;
    }

    /// <summary>Runs <paramref name="sql"/> as the app role, which the service runs as.</summary>
    /// <remarks>For fixed statements only: nothing may be concatenated into them.</remarks>
    internal async Task<object?> ExecuteScalarAsAppRoleAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(AppConnectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        if (await MigrateAsync() != 0)
        {
            throw new InvalidOperationException("migrate failed; its log says why.");
        }

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        jwks = builder.Build();
        var document = JsonSerializer.Serialize(new JsonWebKeySetResponse([PublicJsonWebKey.FromKey(gatewayKey, "test")]));
        jwks.Run(context => context.Response.WriteAsync(document));
        await jwks.StartAsync();

        controlPlane = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host
                .UseSetting($"ConnectionStrings:{ControlPlanePersistenceExtensions.AppConnectionName}", AppConnectionString)
                .UseSetting($"{GatewayTokenOptions.SectionName}:{nameof(GatewayTokenOptions.JwksUri)}", $"{jwks.Urls.Single()}{GatewayTokens.JwksPath}"));
    }

    public async Task DisposeAsync()
    {
        if (controlPlane is not null)
        {
            await controlPlane.DisposeAsync();
        }

        if (jwks is not null)
        {
            await jwks.DisposeAsync();
        }

        gatewayKey.Dispose();
        await postgres.DisposeAsync();
    }

    /// <summary>
    /// Runs <c>migrate</c> as the owner role with the seed of this fixture;
    /// <paramref name="overrides"/> replace single settings, as <c>--Seed:ClientId=</c>.
    /// </summary>
    /// <returns>The exit code.</returns>
    internal Task<int> MigrateAsync(params string[] overrides)
    {
        string[] args =
        [
            $"--ConnectionStrings:{MigrateCommand.OwnerConnectionName}={OwnerConnectionString}",
            $"--{SeedOptions.SectionName}:{nameof(SeedOptions.TenantId)}={Tenant.Id}",
            $"--{SeedOptions.SectionName}:{nameof(SeedOptions.TenantName)}={Tenant.Name}",
            $"--{SeedOptions.SectionName}:{nameof(SeedOptions.PlanId)}={Plan.Id}",
            $"--{SeedOptions.SectionName}:{nameof(SeedOptions.PlanName)}={Plan.Name}",
            $"--{SeedOptions.SectionName}:{nameof(SeedOptions.RequestsPerSecond)}={Plan.RequestsPerSecond}",
            $"--{SeedOptions.SectionName}:{nameof(SeedOptions.Burst)}={Plan.Burst}",
            $"--{SeedOptions.SectionName}:{nameof(SeedOptions.ClientId)}={ClientId}",
        ];

        return MigrateCommand.RunAsync([.. args, .. overrides]);
    }

    private string ConnectionString(string role, string password)
    {
        return new NpgsqlConnectionStringBuilder(postgres.GetConnectionString())
        {
            Username = role,
            Password = password,
        }.ConnectionString;
    }

    private string ServiceToken()
    {
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = GatewayTokens.Issuer,
            Audience = ControlPlaneService.Audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, GatewayTokens.GatewaySubject)]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now + GatewayTokens.MaximumLifetime,
            SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(gatewayKey) { KeyId = "test" }, GatewayTokens.SigningAlgorithm),
        });
    }
}
