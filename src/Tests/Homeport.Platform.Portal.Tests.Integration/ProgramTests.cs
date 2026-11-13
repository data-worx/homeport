// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Homeport.Platform.Portal.Tests.Integration;

public sealed class ProgramTests
{
    public sealed class Main(WebApplicationFactory<Program> portal) : IClassFixture<WebApplicationFactory<Program>>
    {
        // The design kit's files, linked into the portal: it serves them
        // itself, so no page loads anything from a CDN or a font service.
        [Theory]
        [InlineData("/portal.css")]
        [InlineData("/fonts/InstrumentSans-Variable.woff2")]
        [InlineData("/fonts/InstrumentSans-Variable-Italic.woff2")]
        [InlineData("/fonts/JetBrainsMono-Variable.woff2")]
        [InlineData("/favicon.svg")]
        [InlineData("/homeport-icon-mono-white.svg")]
        public async Task Serves_The_Files_Of_The_Design_Kit(string path)
        {
            using var response = await portal.CreateClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Serves_The_Shell()
        {
            var page = await portal.CreateClient().GetStringAsync("/");

            Assert.Contains("Platform admin", page, StringComparison.Ordinal);
        }
    }
}
