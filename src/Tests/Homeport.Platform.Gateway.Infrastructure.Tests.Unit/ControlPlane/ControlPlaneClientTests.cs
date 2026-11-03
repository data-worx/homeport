// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Json;
using Homeport.Platform.Gateway.Infrastructure.ControlPlane;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Infrastructure.Tests.Unit.ControlPlane;

public sealed class ControlPlaneClientTests
{
    private const string KnownClientId = "known-client";

    private const string UnknownClientId = "unknown-client";

    private static readonly ClientResponse KnownClient = new(
        KnownClientId,
        Guid.Parse("3f6c1b9e-2d4a-4e8f-9b7c-5a1d0e2f4c61"),
        new PlanResponse(Guid.Parse("8a2e4c6d-1f3b-4a5c-9d7e-0b1c2d3e4f50"), 10, 20));

    public sealed class FindAsync
    {
        [Fact]
        public async Task Returns_The_Client_The_Control_Plane_Knows()
        {
            var client = Client(new StandInControlPlane());

            var found = await client.FindAsync(KnownClientId, CancellationToken.None);

            Assert.Equal(KnownClient, found);
        }

        [Fact]
        public async Task Returns_Null_For_A_Client_The_Control_Plane_Does_Not_Know()
        {
            var client = Client(new StandInControlPlane());

            var found = await client.FindAsync(UnknownClientId, CancellationToken.None);

            Assert.Null(found);
        }

        [Fact]
        public async Task Asks_The_Control_Plane_Once_Per_Client_Within_The_Cache_Lifetime()
        {
            var controlPlane = new StandInControlPlane();
            var client = Client(controlPlane);

            await client.FindAsync(KnownClientId, CancellationToken.None);
            await client.FindAsync(KnownClientId, CancellationToken.None);

            Assert.Equal([$"/clients/{KnownClientId}"], controlPlane.Paths);
        }

        [Fact]
        public async Task Keeps_An_Unknown_Client_Within_The_Cache_Lifetime()
        {
            var controlPlane = new StandInControlPlane();
            var client = Client(controlPlane);

            await client.FindAsync(UnknownClientId, CancellationToken.None);
            await client.FindAsync(UnknownClientId, CancellationToken.None);

            Assert.Single(controlPlane.Paths);
        }

        [Fact]
        public async Task Asks_The_Control_Plane_For_Each_Client()
        {
            var controlPlane = new StandInControlPlane();
            var client = Client(controlPlane);

            await client.FindAsync(KnownClientId, CancellationToken.None);
            await client.FindAsync(UnknownClientId, CancellationToken.None);

            Assert.Equal([$"/clients/{KnownClientId}", $"/clients/{UnknownClientId}"], controlPlane.Paths);
        }

        [Fact]
        public async Task Asks_The_Control_Plane_Again_Once_The_Cache_Lifetime_Has_Passed()
        {
            var controlPlane = new StandInControlPlane();
            var client = Client(controlPlane, TimeSpan.FromTicks(1));

            await client.FindAsync(KnownClientId, CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            await client.FindAsync(KnownClientId, CancellationToken.None);

            Assert.Equal(2, controlPlane.Paths.Count);
        }

        [Fact]
        public async Task Throws_Unavailable_When_The_Control_Plane_Answers_With_An_Error()
        {
            var client = Client(new StandInControlPlane { FailureStatus = HttpStatusCode.InternalServerError });

            await Assert.ThrowsAsync<ControlPlaneUnavailableException>(() => client.FindAsync(KnownClientId, CancellationToken.None));
        }

        [Fact]
        public async Task Does_Not_Keep_A_Failed_Lookup()
        {
            var controlPlane = new StandInControlPlane { FailureStatus = HttpStatusCode.InternalServerError };
            var client = Client(controlPlane);
            await Assert.ThrowsAsync<ControlPlaneUnavailableException>(() => client.FindAsync(KnownClientId, CancellationToken.None));
            controlPlane.FailureStatus = null;

            var found = await client.FindAsync(KnownClientId, CancellationToken.None);

            Assert.Equal(KnownClient, found);
        }

        [Fact]
        public async Task Escapes_The_Client_Id_In_The_Path()
        {
            var controlPlane = new StandInControlPlane();
            var client = Client(controlPlane);

            await client.FindAsync("a/b c", CancellationToken.None);

            Assert.Equal(["/clients/a%2Fb%20c"], controlPlane.Paths);
        }

        private static ControlPlaneClient Client(StandInControlPlane controlPlane, TimeSpan? cacheLifetime = null)
        {
            var options = new ControlPlaneOptions { Url = new Uri("http://control-plane") };
            if (cacheLifetime is { } lifetime)
            {
                options.ClientCacheLifetime = lifetime;
            }

            return new ControlPlaneClient(
                new SingleClientFactory(controlPlane, options.Url),
                new MemoryCache(new MemoryCacheOptions()),
                Options.Create(options));
        }
    }

    // Knows KnownClient, answers 404 for every other client, or FailureStatus when set.
    private sealed class StandInControlPlane : HttpMessageHandler
    {
        private readonly List<string> paths = [];

        internal HttpStatusCode? FailureStatus { get; set; }

        internal IReadOnlyList<string> Paths => paths;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            paths.Add(path);

            if (FailureStatus is { } status)
            {
                return Task.FromResult(new HttpResponseMessage(status));
            }

            return Task.FromResult(path == $"/clients/{KnownClientId}"
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(KnownClient) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler, Uri baseAddress) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { BaseAddress = baseAddress };
    }
}
