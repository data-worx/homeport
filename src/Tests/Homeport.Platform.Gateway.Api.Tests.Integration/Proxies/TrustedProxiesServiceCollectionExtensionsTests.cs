// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Platform.Gateway.Api.Proxies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.Proxies;

public sealed class TrustedProxiesServiceCollectionExtensionsTests
{
    // What the module sees is what the gateway saw: YARP sets X-Forwarded-For
    // and X-Forwarded-Proto on the forwarded request from the caller's
    // address and the request's scheme.
    public sealed class AddTrustedProxies(ForwardingGateway gateway) : IClassFixture<ForwardingGateway>
    {
        private const string SpoofedAddress = "203.0.113.7";

        private const string SpoofedScheme = "https";

        private static readonly IPAddress Proxy = IPAddress.Parse("192.0.2.20");

        private static readonly IPAddress OtherCaller = IPAddress.Parse("192.0.2.10");

        [Fact]
        public async Task Ignores_Forwarded_Headers_From_A_Caller_That_Is_No_Trusted_Proxy()
        {
            await using var behindProxy = await StartAsync(OtherCaller, host => host.UseSetting($"{TrustedProxiesOptions.SectionName}:Proxies:0", Proxy.ToString()));

            var forwarded = await SendSpoofedAsync(behindProxy);

            Assert.Equal(OtherCaller.ToString(), forwarded.Headers["x-forwarded-for"]);
            Assert.Equal(Uri.UriSchemeHttp, forwarded.Headers["x-forwarded-proto"]);
        }

        [Fact]
        public async Task Ignores_Forwarded_Headers_From_Loopback_When_No_Proxy_Is_Trusted()
        {
            await using var unconfigured = await StartAsync(IPAddress.Loopback, _ => { });

            var forwarded = await SendSpoofedAsync(unconfigured);

            Assert.Equal(IPAddress.Loopback.ToString(), forwarded.Headers["x-forwarded-for"]);
            Assert.Equal(Uri.UriSchemeHttp, forwarded.Headers["x-forwarded-proto"]);
        }

        [Fact]
        public async Task Ignores_Forwarded_Headers_When_The_Host_Enables_Them_And_No_Proxy_Is_Trusted()
        {
            // ASPNETCORE_FORWARDEDHEADERS_ENABLED, which Aspire sets on publish.
            await using var enabled = await StartAsync(OtherCaller, host => host.UseSetting("FORWARDEDHEADERS_ENABLED", bool.TrueString));

            var forwarded = await SendSpoofedAsync(enabled);

            Assert.Equal(OtherCaller.ToString(), forwarded.Headers["x-forwarded-for"]);
            Assert.Equal(Uri.UriSchemeHttp, forwarded.Headers["x-forwarded-proto"]);
        }

        [Fact]
        public async Task Applies_Forwarded_Headers_From_A_Trusted_Proxy()
        {
            await using var behindProxy = await StartAsync(Proxy, host => host.UseSetting($"{TrustedProxiesOptions.SectionName}:Proxies:0", Proxy.ToString()));

            var forwarded = await SendSpoofedAsync(behindProxy);

            Assert.Equal(SpoofedAddress, forwarded.Headers["x-forwarded-for"]);
            Assert.Equal(SpoofedScheme, forwarded.Headers["x-forwarded-proto"]);
        }

        [Fact]
        public async Task Applies_Forwarded_Headers_From_A_Proxy_In_A_Trusted_Network()
        {
            await using var behindProxy = await StartAsync(Proxy, host => host.UseSetting($"{TrustedProxiesOptions.SectionName}:Networks:0", "192.0.2.0/24"));

            var forwarded = await SendSpoofedAsync(behindProxy);

            Assert.Equal(SpoofedAddress, forwarded.Headers["x-forwarded-for"]);
            Assert.Equal(SpoofedScheme, forwarded.Headers["x-forwarded-proto"]);
        }

        [Fact]
        public async Task Does_Not_Start_With_A_Proxy_That_Is_No_Address()
        {
            await using var misconfigured = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host => host
                    .WithUsage().WithTokenBuckets()
                    .WithSigningKey()
                    .WithIdentityProvider(GatewaySettings.UnusedAuthority)
                    .UseSetting($"{TrustedProxiesOptions.SectionName}:Proxies:0", "proxy.example"));

            Assert.Throws<OptionsValidationException>(() => misconfigured.CreateClient());
        }

        [Fact]
        public async Task Does_Not_Start_With_A_Network_That_Is_No_Cidr()
        {
            await using var misconfigured = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host => host
                    .WithUsage().WithTokenBuckets()
                    .WithSigningKey()
                    .WithIdentityProvider(GatewaySettings.UnusedAuthority)
                    .UseSetting($"{TrustedProxiesOptions.SectionName}:Networks:0", "192.0.2.0"));

            Assert.Throws<OptionsValidationException>(() => misconfigured.CreateClient());
        }

        private async Task<WebApplicationFactory<Program>> StartAsync(IPAddress caller, Action<IWebHostBuilder> configure)
        {
            var started = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(host =>
                {
                    host.WithUsage().WithTokenBuckets()
                        .WithSigningKey()
                        .WithRegistry(gateway.Registry)
                        .WithClients(gateway.IdentityProvider, gateway.ControlPlane)
                        .ConfigureTestServices(services => services.AddTransient<IStartupFilter>(_ => new CallerAddress(caller)))
                        .ConfigureLogging(logging => logging.AddFakeLogging());
                    configure(host);
                });
            await ForwardingGateway.WaitForRoutesAsync(started);

            return started;
        }

        private async Task<ForwardedRequest> SendSpoofedAsync(WebApplicationFactory<Program> started)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/modules/echo" + ForwardingGateway.RequestPath);
            request.Headers.Add("X-Forwarded-For", SpoofedAddress);
            request.Headers.Add("X-Forwarded-Proto", SpoofedScheme);

            using var response = await started.CreateClient()
                .WithToken(gateway.IdentityProvider.Issue(StandInControlPlane.KnownClientId))
                .SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ForwardedRequest>()
                ?? throw new InvalidOperationException("The stand-in module answered without a body.");
        }
    }
}
