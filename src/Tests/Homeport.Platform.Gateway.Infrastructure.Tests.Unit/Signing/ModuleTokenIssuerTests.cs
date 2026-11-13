// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Homeport.Platform.Gateway.Infrastructure.Signing;
using Homeport.Shared.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.Signing;

public sealed class ModuleTokenIssuerTests
{
    public sealed class Issue : IDisposable
    {
        private static readonly ModuleCaller Caller = new(
            "echo",
            Guid.Parse("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61"),
            "local-client",
            "service-account-local-client",
            ["echo.invoke", "usage.read"]);

        private static readonly OperatorCaller Operator = new(
            "homeport-control-plane",
            "2b8d7c1e-4f6a-4b3d-8e9f-0a1b2c3d4e5f",
            "Ada Operator");

        private readonly SigningKeys keys = TestKeys.Keys();
        private readonly SteppingTimeProvider time = new();

        [Fact]
        public async Task Issues_A_Token_The_Module_Accepts_Against_The_Jwks()
        {
            var token = Issuer().Issue(Caller);

            var result = await new JsonWebTokenHandler().ValidateTokenAsync(
                token,
                GatewayTokens.ValidationParameters("echo", new JsonWebKeySet(JsonSerializer.Serialize(keys.Jwks)).GetSigningKeys()));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Carries_Tenant_Client_Subject_And_Scopes()
        {
            var token = new JsonWebToken(Issuer().Issue(Caller));

            Assert.Equal("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61", token.GetClaim("tenant_id").Value);
            Assert.Equal("local-client", token.GetClaim("client_id").Value);
            Assert.Equal("service-account-local-client", token.Subject);
            Assert.Equal("echo.invoke usage.read", token.GetClaim("scope").Value);
        }

        [Fact]
        public void Issues_A_Token_For_The_Configured_Lifetime()
        {
            var token = new JsonWebToken(Issuer(TimeSpan.FromSeconds(30)).Issue(Caller));

            Assert.Equal(TimeSpan.FromSeconds(30), token.ValidTo - token.IssuedAt);
        }

        [Fact]
        public void Issues_A_Token_For_Sixty_Seconds_By_Default()
        {
            var token = new JsonWebToken(Issuer().Issue(Caller));

            Assert.Equal(TimeSpan.FromSeconds(60), token.ValidTo - token.IssuedAt);
        }

        [Fact]
        public async Task Issues_An_Operator_Token_The_Service_Accepts_Against_The_Jwks()
        {
            var token = Issuer().Issue(Operator);

            var result = await new JsonWebTokenHandler().ValidateTokenAsync(
                token,
                GatewayTokens.ValidationParameters("homeport-control-plane", new JsonWebKeySet(JsonSerializer.Serialize(keys.Jwks)).GetSigningKeys()));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Carries_The_Operators_Subject_Name_And_The_Admin_Scope()
        {
            var token = new JsonWebToken(Issuer().Issue(Operator));

            Assert.Equal(
                ("2b8d7c1e-4f6a-4b3d-8e9f-0a1b2c3d4e5f", "Ada Operator", "admin"),
                (token.Subject, token.GetClaim("name").Value, token.GetClaim("scope").Value));
        }

        [Fact]
        public void Names_Neither_Tenant_Nor_Client_In_An_Operator_Token()
        {
            var token = new JsonWebToken(Issuer().Issue(Operator));

            Assert.False(token.TryGetClaim("tenant_id", out _) || token.TryGetClaim("client_id", out _));
        }

        [Fact]
        public void Issues_An_Operator_Token_For_The_Configured_Lifetime()
        {
            var token = new JsonWebToken(Issuer(TimeSpan.FromSeconds(30)).Issue(Operator));

            Assert.Equal(TimeSpan.FromSeconds(30), token.ValidTo - token.IssuedAt);
        }

        public void Dispose()
        {
            keys.Dispose();
        }

        private ModuleTokenIssuer Issuer(TimeSpan? lifetime = null)
        {
            var options = new ModuleTokenOptions();
            if (lifetime is not null)
            {
                options.Lifetime = lifetime.Value;
            }

            return new ModuleTokenIssuer(keys, Microsoft.Extensions.Options.Options.Create(options), time);
        }
    }
}
