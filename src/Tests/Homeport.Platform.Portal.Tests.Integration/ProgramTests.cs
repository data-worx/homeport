// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;

namespace Homeport.Platform.Portal.Tests.Integration;

public sealed class ProgramTests
{
    public sealed class Main(PortalFactory portal) : IClassFixture<PortalFactory>
    {
        // The design kit's files, linked into the portal: it serves them
        // itself, to anonymous callers too, so no page loads anything from a
        // CDN or a font service.
        [Theory]
        [InlineData("/portal.css")]
        [InlineData("/fonts/InstrumentSans-Variable.woff2")]
        [InlineData("/fonts/InstrumentSans-Variable-Italic.woff2")]
        [InlineData("/fonts/JetBrainsMono-Variable.woff2")]
        [InlineData("/favicon.svg")]
        [InlineData("/homeport-icon-mono-white.svg")]
        [InlineData("/homeport-lockup.svg")]
        [InlineData("/hero-home-port.svg")]
        public async Task Serves_The_Files_Of_The_Design_Kit_To_Anyone(string path)
        {
            using var response = await portal.CreateClient(PortalFactory.HttpsAddress).GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Serves_The_Shell_With_The_Operators_Name()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/");
            request.Headers.Add(PortalFactory.TestUser.Header, PortalFactory.TestUser.Operator);

            using var response = await portal.CreateClient(PortalFactory.HttpsAddress).SendAsync(request);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains(PortalFactory.TestUser.OperatorName, await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        }

        [Fact]
        public async Task Answers_A_Missing_Page_With_The_Not_Found_Page_And_The_Trace_Id()
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/no-such-page");
            request.Headers.Add(PortalFactory.TestUser.Header, PortalFactory.TestUser.Operator);
            request.Headers.Add(TraceParent.Header, TraceParent.Value);

            using var response = await portal.CreateClient(PortalFactory.HttpsAddress).SendAsync(request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            var page = await response.Content.ReadAsStringAsync();
            Assert.Contains("Page not found", page, StringComparison.Ordinal);
            Assert.Contains(TraceParent.TraceId, page, StringComparison.Ordinal);
        }
    }
}
