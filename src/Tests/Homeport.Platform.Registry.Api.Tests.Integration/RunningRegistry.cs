// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Homeport.Platform.Registry.Infrastructure;
using Homeport.Platform.Registry.Infrastructure.Keys;
using Homeport.Shared.Authentication;
using Homeport.Shared.Internal.Platform.Registry;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace Homeport.Platform.Registry.Api.Tests.Integration;

/// <summary>
/// The registry in memory, against a throwaway Valkey and a stand-in for the
/// gateway's JWKS on a local port. It knows the module <see cref="ModuleId"/>
/// with the key <see cref="RegistrationKey"/>. Needs a running Docker.
/// </summary>
public sealed class RunningRegistry : IAsyncLifetime
{
    internal const string ModuleId = "echo";

    internal const string RegistrationKey = "echo-registration-key";

    private const string LocalAddress = "http://127.0.0.1:0";

    // Pinned like the AppHost pins it.
    private const string ValkeyImage = "valkey/valkey:9.1@sha256:418652cfb58ef879d4978c33553735d7147016032d5aefaa14c828e611eb9dfd";

    private readonly RedisContainer valkey = new RedisBuilder(ValkeyImage).Build();
    private readonly ECDsa gatewayKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private WebApplication? jwks;
    private WebApplicationFactory<Program>? registry;

    private WebApplicationFactory<Program> Started =>
        registry ?? throw new InvalidOperationException("The registry has not been started.");

    /// <summary>What the registry logged.</summary>
    internal FakeLogCollector Logs => Started.Services.GetFakeLogCollector();

    /// <summary>A client without credentials.</summary>
    internal HttpClient CreateClient() => Started.CreateClient();

    /// <summary>A client that sends <paramref name="key"/> as registration key.</summary>
    internal HttpClient CreateModuleClient(string key = RegistrationKey)
    {
        var client = Started.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, key);
        return client;
    }

    /// <summary>A client that sends a gateway service token for the registry.</summary>
    internal HttpClient CreateGatewayClient()
    {
        var client = Started.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JwtBearerDefaults.AuthenticationScheme, ServiceToken());
        return client;
    }

    /// <summary>Removes every registered instance, so a test starts from an empty registry.</summary>
    internal async Task FlushAsync()
    {
        await using var connection = await ConnectionMultiplexer.ConnectAsync($"{valkey.GetConnectionString()},allowAdmin=true");
        foreach (var server in connection.GetServers())
        {
            await server.FlushDatabaseAsync();
        }
    }

    public async Task InitializeAsync()
    {
        await valkey.StartAsync();

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        jwks = builder.Build();
        var document = JsonSerializer.Serialize(new JsonWebKeySetResponse([PublicJsonWebKey.FromKey(gatewayKey, "test")]));
        jwks.Run(context => context.Response.WriteAsync(document));
        await jwks.StartAsync();

        registry = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host
                .UseSetting($"ConnectionStrings:{RegistryInfrastructureExtensions.ValkeyConnectionName}", valkey.GetConnectionString())
                .UseSetting($"{RegistrationKeyOptions.SectionName}:{nameof(RegistrationKeyOptions.RegistrationKeys)}:{ModuleId}:0", RegistrationKey)
                .UseSetting($"{GatewayTokenOptions.SectionName}:{nameof(GatewayTokenOptions.JwksUri)}", $"{jwks.Urls.Single()}{GatewayTokens.JwksPath}")
                .ConfigureLogging(logging => logging.AddFakeLogging()));
    }

    public async Task DisposeAsync()
    {
        if (registry is not null)
        {
            await registry.DisposeAsync();
        }

        if (jwks is not null)
        {
            await jwks.DisposeAsync();
        }

        gatewayKey.Dispose();
        await valkey.DisposeAsync();
    }

    private string ServiceToken()
    {
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = GatewayTokens.Issuer,
            Audience = RegistryService.Audience,
            Subject = new ClaimsIdentity([new Claim(JwtRegisteredClaimNames.Sub, GatewayTokens.GatewaySubject)]),
            IssuedAt = now,
            NotBefore = now,
            Expires = now + GatewayTokens.MaximumLifetime,
            SigningCredentials = new SigningCredentials(new ECDsaSecurityKey(gatewayKey) { KeyId = "test" }, GatewayTokens.SigningAlgorithm),
        });
    }
}
