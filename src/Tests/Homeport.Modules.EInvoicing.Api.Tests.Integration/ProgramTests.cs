// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Homeport.Sdk;
using Homeport.Shared.Client.ModuleContract;

namespace Homeport.Modules.EInvoicing.Api.Tests.Integration;

public sealed class ProgramTests
{
    public sealed class Main(EInvoicingBehindGateway module) : IClassFixture<EInvoicingBehindGateway>
    {
        [Fact]
        public async Task Declares_The_Validate_And_Read_Routes_With_Their_Scopes_And_The_Documents_Meter()
        {
            var response = await module.CreateClient().GetStringAsync("/.well-known/homeport/manifest");

            var root = JsonDocument.Parse(response).RootElement;
            Assert.Equal("e-invoicing", root.GetProperty("id").GetString());
            Assert.Equal(
                """[{"method":"POST","path":"/v1/validate","scope":"e-invoicing.validate","meter":"documents","quantity":1},{"method":"POST","path":"/v1/read","scope":"e-invoicing.read","meter":"documents","quantity":1}]""",
                root.GetProperty("routes").GetRawText());
        }

        [Fact]
        public async Task Documents_Every_Operation_For_A_Caller()
        {
            var document = await OpenApiDocument();

            Assert.Empty(ModuleOpenApi.Validate(document));
        }

        [Theory]
        [InlineData("DocumentContainer", """["xml","pdf"]""")]
        // Nullable, as the report's syntax is: a PDF has none yet.
        [InlineData("InvoiceSyntax", """["ubl-invoice","ubl-credit-note","cii",null]""")]
        public async Task Lists_The_Wire_Values_Of_An_Enum(string schema, string values)
        {
            var document = await OpenApiDocument();

            var found = document.GetProperty("components").GetProperty("schemas").GetProperty(schema).GetProperty("enum");
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(values), JsonNode.Parse(found.GetRawText())), found.GetRawText());
        }

        [Theory]
        [InlineData("/health/live")]
        [InlineData("/health/ready")]
        public async Task Answers_Health(string path)
        {
            var response = await module.CreateClient().GetAsync(path);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        private async Task<JsonElement> OpenApiDocument()
        {
            var document = await module.CreateClient().GetStringAsync(ModuleManifest.OpenApiPath);

            return JsonDocument.Parse(document).RootElement.Clone();
        }
    }
}
