// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Shared.ServiceDefaults.Proxies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Homeport.Shared.ServiceDefaults.Tests.Integration.Proxies;

public sealed class TrustedProxiesServiceCollectionExtensionsTests
{
    // The address and scheme the service sees once the forwarded headers
    // middleware has run.
    public sealed class AddTrustedProxies
    {
        private const string CallerPath = "/caller";

        private const string SpoofedAddress = "203.0.113.7";

        private const string SpoofedScheme = "https";

        private static readonly IPAddress Proxy = IPAddress.Parse("192.0.2.20");

        private static readonly IPAddress OtherCaller = IPAddress.Parse("192.0.2.10");

        [Fact]
        public async Task Ignores_Forwarded_Headers_From_A_Caller_That_Is_No_Trusted_Proxy()
        {
            await using var behindProxy = await StartAsync(OtherCaller, ($"{TrustedProxiesOptions.SectionName}:Proxies:0", Proxy.ToString()));

            var seen = await SendSpoofedAsync(behindProxy);

            Assert.Equal(new SeenCaller(OtherCaller.ToString(), Uri.UriSchemeHttp), seen);
        }

        [Fact]
        public async Task Ignores_Forwarded_Headers_From_Loopback_When_No_Proxy_Is_Trusted()
        {
            await using var unconfigured = await StartAsync(IPAddress.Loopback);

            var seen = await SendSpoofedAsync(unconfigured);

            Assert.Equal(new SeenCaller(IPAddress.Loopback.ToString(), Uri.UriSchemeHttp), seen);
        }

        [Fact]
        public async Task Ignores_Forwarded_Headers_When_The_Host_Enables_Them_And_No_Proxy_Is_Trusted()
        {
            // ASPNETCORE_FORWARDEDHEADERS_ENABLED, which Aspire sets on publish.
            await using var enabled = await StartAsync(OtherCaller, ("FORWARDEDHEADERS_ENABLED", bool.TrueString));

            var seen = await SendSpoofedAsync(enabled);

            Assert.Equal(new SeenCaller(OtherCaller.ToString(), Uri.UriSchemeHttp), seen);
        }

        [Fact]
        public async Task Applies_Forwarded_Headers_From_A_Trusted_Proxy()
        {
            await using var behindProxy = await StartAsync(Proxy, ($"{TrustedProxiesOptions.SectionName}:Proxies:0", Proxy.ToString()));

            var seen = await SendSpoofedAsync(behindProxy);

            Assert.Equal(new SeenCaller(SpoofedAddress, SpoofedScheme), seen);
        }

        [Fact]
        public async Task Applies_Forwarded_Headers_From_A_Proxy_In_A_Trusted_Network()
        {
            await using var behindProxy = await StartAsync(Proxy, ($"{TrustedProxiesOptions.SectionName}:Networks:0", "192.0.2.0/24"));

            var seen = await SendSpoofedAsync(behindProxy);

            Assert.Equal(new SeenCaller(SpoofedAddress, SpoofedScheme), seen);
        }

        [Fact]
        public async Task Does_Not_Start_With_A_Proxy_That_Is_No_Address()
        {
            await Assert.ThrowsAsync<OptionsValidationException>(
                () => StartAsync(OtherCaller, ($"{TrustedProxiesOptions.SectionName}:Proxies:0", "proxy.example")));
        }

        [Fact]
        public async Task Does_Not_Start_With_A_Network_That_Is_No_Cidr()
        {
            await Assert.ThrowsAsync<OptionsValidationException>(
                () => StartAsync(OtherCaller, ($"{TrustedProxiesOptions.SectionName}:Networks:0", "192.0.2.0")));
        }

        private static async Task<WebApplication> StartAsync(IPAddress caller, params (string Key, string Value)[] settings)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = Environments.Production,
            });
            builder.WebHost.UseTestServer();
            foreach (var (key, value) in settings)
            {
                builder.Configuration[key] = value;
            }

            builder.Services.AddTrustedProxies();
            builder.Services.AddTransient<IStartupFilter>(_ => new CallerAddress(caller));

            var app = builder.Build();
            app.UseForwardedHeaders();
            app.MapGet(CallerPath, (HttpContext context) =>
                new SeenCaller(context.Connection.RemoteIpAddress?.ToString(), context.Request.Scheme));

            try
            {
                await app.StartAsync();
            }
            catch
            {
                await app.DisposeAsync();
                throw;
            }

            return app;
        }

        private static async Task<SeenCaller> SendSpoofedAsync(WebApplication app)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, CallerPath);
            request.Headers.Add("X-Forwarded-For", SpoofedAddress);
            request.Headers.Add("X-Forwarded-Proto", SpoofedScheme);

            using var response = await app.GetTestClient().SendAsync(request);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<SeenCaller>()
                ?? throw new InvalidOperationException("The service answered without a body.");
        }

        private sealed record SeenCaller(string? Address, string Scheme);
    }
}
