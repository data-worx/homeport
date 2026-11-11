// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Cryptography;
using System.Text.Json;
using Homeport.Shared.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Testing.Gateway;

/// <summary>
/// A stand-in for the gateway on a local port: it publishes the JWKS of its
/// key and signs module tokens with it the way the gateway does.
/// </summary>
public sealed class StandInGateway : IAsyncDisposable
{
    public const string TenantId = "3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61";

    public const string ClientId = "local-client";

    public const string Subject = "service-account-local-client";

    private const string LocalAddress = "http://127.0.0.1:0";

    private const string KeyId = "current";

    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    private readonly ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private WebApplication? app;

    /// <summary>Where the stand-in publishes its JWKS, for <c>GatewayTokens:JwksUri</c>.</summary>
    public string JwksUri => (app?.Urls.Single() ?? throw new InvalidOperationException("The stand-in has not been started.")) + GatewayTokens.JwksPath;

    public static async Task<StandInGateway> StartAsync()
    {
        var gateway = new StandInGateway();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        gateway.app = builder.Build();
        gateway.app.MapGet(GatewayTokens.JwksPath, () => Results.Text(
            JsonSerializer.Serialize(new JsonWebKeySetResponse([PublicJsonWebKey.FromKey(gateway.key, KeyId)])),
            "application/json"));
        await gateway.app.StartAsync();

        return gateway;
    }

    /// <summary>A module token for <paramref name="audience"/> granting <paramref name="scopes"/>.</summary>
    public string Issue(string audience, string scopes, TimeSpan? expiresIn = null)
    {
        return Sign(audience, scopes, expiresIn ?? Lifetime, key);
    }

    /// <summary>A token like <see cref="Issue"/>, but signed with a key the gateway does not publish.</summary>
    public static string IssueWithForeignKey(string audience, string scopes)
    {
        using var foreign = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return Sign(audience, scopes, Lifetime, foreign);
    }

    public async ValueTask DisposeAsync()
    {
        if (app is not null)
        {
            await app.StopAsync();
            await app.DisposeAsync();
        }

        key.Dispose();
    }

    private static string Sign(string audience, string scopes, TimeSpan expiresIn, ECDsa signingKey)
    {
        var expires = TimeProvider.System.GetUtcNow().UtcDateTime + expiresIn;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = GatewayTokens.Issuer,
            Audience = audience,
            Claims = new Dictionary<string, object>
            {
                [GatewayTokens.SubjectClaim] = Subject,
                [GatewayTokens.TenantIdClaim] = TenantId,
                [GatewayTokens.ClientIdClaim] = ClientId,
                [GatewayTokens.ScopeClaim] = scopes,
            },
            IssuedAt = expires - Lifetime,
            NotBefore = expires - Lifetime,
            Expires = expires,
            SigningCredentials = new SigningCredentials(
                new ECDsaSecurityKey(signingKey) { KeyId = KeyId },
                GatewayTokens.SigningAlgorithm),
        });
    }
}
