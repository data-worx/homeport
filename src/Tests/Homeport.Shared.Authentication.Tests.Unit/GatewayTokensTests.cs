// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Homeport.Shared.Authentication.Tests.Unit;

public sealed class GatewayTokensTests
{
    public sealed class ValidationParameters : IDisposable
    {
        private const string Audience = "homeport-registry";

        private readonly TestKey current = new("current");
        private readonly TestKey previous = new("previous");
        private readonly TestKey unknown = new("unknown");

        [Fact]
        public async Task Accepts_A_Token_For_Its_Audience()
        {
            var result = await Validate(current.Sign(Audience), current);

            Assert.True(result.IsValid);
        }

        [Fact]
        public async Task Carries_The_Gateway_As_Subject()
        {
            var result = await Validate(current.Sign(Audience), current);

            Assert.Equal(GatewayTokens.GatewaySubject, result.Claims[JwtRegisteredClaimNames.Sub]);
        }

        [Fact]
        public async Task Rejects_A_Token_For_Another_Audience()
        {
            var result = await Validate(current.Sign("homeport-control-plane"), current);

            Assert.IsType<SecurityTokenInvalidAudienceException>(result.Exception);
        }

        [Fact]
        public async Task Rejects_An_Expired_Token()
        {
            var result = await Validate(current.Sign(Audience, expiresIn: TimeSpan.FromMinutes(-1)), current);

            Assert.IsType<SecurityTokenExpiredException>(result.Exception);
        }

        [Fact]
        public async Task Rejects_A_Token_Signed_By_An_Unknown_Key()
        {
            var result = await Validate(unknown.Sign(Audience), current, previous);

            Assert.IsType<SecurityTokenSignatureKeyNotFoundException>(result.Exception);
        }

        [Fact]
        public async Task Rejects_A_Token_From_Another_Issuer()
        {
            var result = await Validate(current.Sign(Audience, issuer: "someone-else"), current);

            Assert.IsType<SecurityTokenInvalidIssuerException>(result.Exception);
        }

        [Fact]
        public async Task Rejects_An_Unsigned_Token()
        {
            var unsigned = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
            {
                Issuer = GatewayTokens.Issuer,
                Audience = Audience,
            });

            var result = await Validate(unsigned, current);

            Assert.False(result.IsValid);
        }

        [Fact]
        public async Task Accepts_A_Token_Signed_By_The_Previous_Key_While_It_Is_Published()
        {
            var result = await Validate(previous.Sign(Audience), current, previous);

            Assert.True(result.IsValid);
        }

        public void Dispose()
        {
            current.Dispose();
            previous.Dispose();
            unknown.Dispose();
        }

        private static Task<TokenValidationResult> Validate(string token, params TestKey[] published)
        {
            return new JsonWebTokenHandler().ValidateTokenAsync(
                token,
                GatewayTokens.ValidationParameters(Audience, TestKey.SigningKeys(published)));
        }
    }
}
