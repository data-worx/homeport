// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.ApiDocs;

public sealed class ApiDocsServiceCollectionExtensionsTests
{
    public sealed class AddApiDocs
    {
        [Theory]
        [InlineData("/platform/docs")]
        [InlineData("/platform/docs/openapi/platform.json")]
        public async Task Answers_Not_Found_Outside_Development_Without_The_Setting(string path)
        {
            await using var gateway = Gateway("Production");

            var response = await gateway.CreateClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Serves_The_Docs_Outside_Development_When_Switched_On()
        {
            await using var gateway = Gateway("Production", enabled: "true");

            var response = await gateway.CreateClient().GetAsync("/platform/docs/openapi/platform.json");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Answers_Not_Found_In_Development_When_Switched_Off()
        {
            await using var gateway = Gateway("Development", enabled: "false");

            var response = await gateway.CreateClient().GetAsync("/platform/docs");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private static WebApplicationFactory<Program> Gateway(string environment, string? enabled = null)
        {
            return new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
            {
                host.UseEnvironment(environment).WithUsage().WithTokenBuckets().WithSigningKey().WithIdentityProvider(GatewaySettings.UnusedAuthority);
                if (enabled is not null)
                {
                    host.UseSetting("Gateway:Docs:Enabled", enabled);
                }
            });
        }
    }
}
