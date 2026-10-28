// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Homeport.Platform.Gateway.Infrastructure.Signing;
using Homeport.Shared.Authentication;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.Signing;

public sealed class ServiceTokenIssuerTests
{
    public sealed class Issue : IDisposable
    {
        private const string Audience = "homeport-registry";

        private readonly SigningKeys keys = TestKeys.Keys();
        private readonly SteppingTimeProvider time = new();
        private readonly ServiceTokenIssuer issuer;

        public Issue()
        {
            issuer = new ServiceTokenIssuer(keys, time);
        }

        [Fact]
        public async Task Issues_A_Token_Its_Audience_Accepts_Against_The_Jwks()
        {
            var token = issuer.Issue(Audience);

            var result = await new JsonWebTokenHandler().ValidateTokenAsync(
                token,
                GatewayTokens.ValidationParameters(Audience, new JsonWebKeySet(JsonSerializer.Serialize(keys.Jwks)).GetSigningKeys()));

            Assert.True(result.IsValid);
        }

        [Fact]
        public void Names_The_Signing_Key_Id()
        {
            var token = new JsonWebToken(issuer.Issue(Audience));

            Assert.Equal("current", token.Kid);
        }

        [Fact]
        public void Issues_A_Token_For_The_Maximum_Lifetime()
        {
            var token = new JsonWebToken(issuer.Issue(Audience));

            Assert.Equal(GatewayTokens.MaximumLifetime, token.ValidTo - token.IssuedAt);
        }

        [Fact]
        public void Reuses_The_Token_Until_It_Is_Due_For_Renewal()
        {
            var first = issuer.Issue(Audience);
            time.Advance(TimeSpan.FromMinutes(3));

            var second = issuer.Issue(Audience);

            Assert.Equal(first, second);
        }

        [Fact]
        public void Issues_A_New_Token_Once_The_Old_One_Is_Due_For_Renewal()
        {
            var first = issuer.Issue(Audience);
            time.Advance(TimeSpan.FromMinutes(4.5));

            var second = issuer.Issue(Audience);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void Issues_A_Token_Per_Audience()
        {
            var registry = new JsonWebToken(issuer.Issue(Audience));

            var controlPlane = new JsonWebToken(issuer.Issue("homeport-control-plane"));

            Assert.Equal(["homeport-control-plane"], controlPlane.Audiences);
            Assert.NotEqual(registry.EncodedToken, controlPlane.EncodedToken);
        }

        public void Dispose()
        {
            keys.Dispose();
        }
    }
}
