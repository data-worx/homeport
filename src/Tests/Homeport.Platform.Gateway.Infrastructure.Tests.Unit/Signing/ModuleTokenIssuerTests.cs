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
