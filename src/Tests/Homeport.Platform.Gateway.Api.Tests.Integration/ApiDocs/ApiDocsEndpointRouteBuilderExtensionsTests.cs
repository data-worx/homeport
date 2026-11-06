// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Mime;
using System.Text.RegularExpressions;

namespace Homeport.Platform.Gateway.Api.Tests.Integration.ApiDocs;

public sealed partial class ApiDocsEndpointRouteBuilderExtensionsTests
{
    // Every src and href attribute, and every url(...) of an inline style.
    [GeneratedRegex("""(?:src|href)\s*=\s*["'](?<url>[^"']+)["']|url\(\s*["']?(?<url>[^"')]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex ResourceUrl();

    public sealed class MapApiDocs(DocumentingGateway docs) : IClassFixture<DocumentingGateway>
    {
        private const string ViewPath = "/platform/docs";

        [Fact]
        public async Task Serves_The_View_Without_A_Token()
        {
            var response = await docs.Gateway.CreateClient().GetAsync(ViewPath);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Text.Html, response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Lists_The_Platform_And_Every_Module()
        {
            var html = await docs.Gateway.CreateClient().GetStringAsync(ViewPath);

            Assert.Contains("""{"title":"platform","url":"platform/docs/openapi/platform.json"}""", html, StringComparison.Ordinal);
            Assert.Contains("""{"title":"echo","url":"platform/docs/openapi/echo.json"}""", html, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Loads_No_Resource_From_Another_Host()
        {
            var html = await docs.Gateway.CreateClient().GetStringAsync(ViewPath);

            var external = ResourceUrl().Matches(html).Select(match => match.Groups["url"].Value).Where(IsAbsolute);

            Assert.Empty(external);
        }

        [Theory]
        [InlineData("\"withDefaultFonts\":false")]
        [InlineData("\"telemetry\":false")]
        [InlineData("\"agent\":{\"disabled\":true}")]
        [InlineData("\"mcp\":{\"disabled\":true}")]
        [InlineData("\"hideClientButton\":true")]
        [InlineData("\"showDeveloperTools\":\"never\"")]
        public async Task Switches_Off_What_Reaches_Scalar_Com(string setting)
        {
            var html = await docs.Gateway.CreateClient().GetStringAsync(ViewPath);

            Assert.Contains(setting, html, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Serves_The_Scalar_Script_Itself()
        {
            var response = await docs.Gateway.CreateClient().GetAsync(ViewPath + "/scalar.js");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private static bool IsAbsolute(string url) =>
            url.StartsWith("//", StringComparison.Ordinal)
            || (Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https");
    }
}
