// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Controllers;

public sealed class JwksControllerTests
{
    public sealed class Get
    {
        private const string JwksPath = "/.well-known/jwks.json";

        [Fact]
        public async Task Returns_The_Signing_Key_With_Its_Key_Id_Without_A_Token()
        {
            await using var gateway = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host => host.WithSigningKey());

            var keyIds = await KeyIds(gateway);

            Assert.Equal([SigningSettings.KeyId], keyIds);
        }

        [Fact]
        public async Task Returns_The_Additional_Key_After_The_Signing_Key_When_Configured()
        {
            await using var gateway = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host => host.WithSigningKey().WithAdditionalPublicKey());

            var keyIds = await KeyIds(gateway);

            Assert.Equal([SigningSettings.KeyId, SigningSettings.AdditionalKeyId], keyIds);
        }

        [Fact]
        public async Task Publishes_Es256_Keys_On_P256()
        {
            await using var gateway = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host => host.WithSigningKey());

            var key = (await Keys(gateway)).Single();

            Assert.Equal(["EC", "sig", "ES256", "P-256"], [Text(key, "kty"), Text(key, "use"), Text(key, "alg"), Text(key, "crv")]);
        }

        private static async Task<List<JsonElement>> Keys(WebApplicationFactory<Program> gateway)
        {
            var json = await gateway.CreateClient().GetStringAsync(JwksPath);

            return JsonDocument.Parse(json).RootElement.GetProperty("keys").EnumerateArray().ToList();
        }

        private static async Task<List<string>> KeyIds(WebApplicationFactory<Program> gateway)
        {
            return (await Keys(gateway)).Select(key => Text(key, "kid")).ToList();
        }

        private static string Text(JsonElement key, string member) =>
            key.GetProperty(member).GetString() ?? string.Empty;
    }
}
