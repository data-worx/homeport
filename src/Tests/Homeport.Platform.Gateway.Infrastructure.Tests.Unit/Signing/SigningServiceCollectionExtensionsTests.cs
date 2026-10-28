// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using Homeport.Platform.Gateway.Infrastructure.Signing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.Signing;

public sealed class SigningServiceCollectionExtensionsTests
{
    public sealed class AddGatewaySigning
    {
        [Fact]
        public void Refuses_Options_Without_A_Signing_Key()
        {
            using var services = Services(new Dictionary<string, string?>());

            Assert.Throws<OptionsValidationException>(() => services.GetRequiredService<IOptions<SigningOptions>>().Value);
        }
    }

    public sealed class AddServiceToken
    {
        private const string ClientName = "registry";
        private const string Audience = "homeport-registry";

        [Fact]
        public async Task Sends_The_Token_As_Bearer()
        {
            var authorization = await AuthorizationSent();

            Assert.Equal("Bearer", authorization?.Scheme);
        }

        [Fact]
        public async Task Sends_A_Token_For_The_Audience_Of_The_Client()
        {
            var authorization = await AuthorizationSent();

            Assert.Equal([Audience], new JsonWebToken(authorization?.Parameter).Audiences);
        }

        private static async Task<AuthenticationHeaderValue?> AuthorizationSent()
        {
            var recorder = new RecordingHandler();
            using var services = Services(
                new Dictionary<string, string?> { ["Signing:Key"] = TestKeys.PrivateP256(), ["Signing:KeyId"] = "current" },
                collection => collection.AddHttpClient(ClientName)
                    .AddServiceToken(Audience)
                    .ConfigurePrimaryHttpMessageHandler(() => recorder));

            await services.GetRequiredService<IHttpClientFactory>().CreateClient(ClientName).GetAsync(new Uri("http://registry/routes"));

            return recorder.Authorization;
        }
    }

    private static ServiceProvider Services(
        Dictionary<string, string?> configuration,
        Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(configuration).Build());
        services.AddGatewaySigning();
        configure?.Invoke(services);

        return services.BuildServiceProvider();
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        internal AuthenticationHeaderValue? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
