// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>
/// A stand-in for the operator's identity provider on a local port: it serves
/// OpenID Connect metadata and the JWKS of its key, and issues
/// client-credentials tokens signed with that key.
/// </summary>
internal sealed class StandInIdentityProvider : IAsyncDisposable
{
    internal const string Audience = "homeport";

    internal const string ClientIdClaim = "azp";

    private const string LocalAddress = "http://127.0.0.1:0";

    private const string MetadataPath = "/.well-known/openid-configuration";

    private const string JwksPath = "/jwks";

    private const string KeyId = "stand-in";

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private readonly ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private WebApplication? app;

    /// <summary>The issuer, which is also the authority the gateway is configured with.</summary>
    internal string Issuer => app?.Urls.Single() ?? throw new InvalidOperationException("The stand-in has not been started.");

    internal static async Task<StandInIdentityProvider> StartAsync()
    {
        var provider = new StandInIdentityProvider();
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        provider.app = builder.Build();
        provider.app.MapGet(MetadataPath, provider.Metadata);
        provider.app.MapGet(JwksPath, provider.Jwks);
        await provider.app.StartAsync();

        return provider;
    }

    /// <summary>A token of <paramref name="clientId"/> for <see cref="Audience"/>, signed with this provider's key.</summary>
    internal string Issue(string clientId)
    {
        return Sign(clientId, Issuer, Audience, new ECDsaSecurityKey(key) { KeyId = KeyId });
    }

    /// <summary>A token like <see cref="Issue"/>, but signed with a key the provider does not publish.</summary>
    internal string IssueWithForeignKey(string clientId)
    {
        using var foreign = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return Sign(clientId, Issuer, Audience, new ECDsaSecurityKey(foreign) { KeyId = KeyId });
    }

    /// <summary>A token like <see cref="Issue"/>, but for another audience.</summary>
    internal string IssueForAudience(string clientId, string audience)
    {
        return Sign(clientId, Issuer, audience, new ECDsaSecurityKey(key) { KeyId = KeyId });
    }

    /// <summary>A token like <see cref="Issue"/>, but without the client id claim.</summary>
    internal string IssueWithoutClientId()
    {
        return Sign(null, Issuer, Audience, new ECDsaSecurityKey(key) { KeyId = KeyId });
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

    private static string Sign(string? clientId, string issuer, string audience, ECDsaSecurityKey signingKey)
    {
        var now = TimeProvider.System.GetUtcNow().UtcDateTime;
        var claims = new ClaimsIdentity();
        if (clientId is not null)
        {
            claims.AddClaim(new Claim(ClientIdClaim, clientId));
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Subject = claims,
            IssuedAt = now,
            NotBefore = now,
            Expires = now + Lifetime,
            SigningCredentials = new SigningCredentials(signingKey, SecurityAlgorithms.EcdsaSha256),
        });
    }

    private IResult Metadata()
    {
        return Results.Json(new Dictionary<string, object>
        {
            ["issuer"] = Issuer,
            ["jwks_uri"] = Issuer + JwksPath,
            ["id_token_signing_alg_values_supported"] = new[] { SecurityAlgorithms.EcdsaSha256 },
        });
    }

    private IResult Jwks()
    {
        var parameters = key.ExportParameters(includePrivateParameters: false);

        return Results.Json(new Dictionary<string, object>
        {
            ["keys"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["kty"] = "EC",
                    ["crv"] = "P-256",
                    ["kid"] = KeyId,
                    ["use"] = "sig",
                    ["alg"] = SecurityAlgorithms.EcdsaSha256,
                    ["x"] = Base64UrlEncoder.Encode(parameters.Q.X),
                    ["y"] = Base64UrlEncoder.Encode(parameters.Q.Y),
                },
            },
        });
    }
}
