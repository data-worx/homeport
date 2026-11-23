// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Text;
using System.Text.Json;
using Homeport.Shared.Client.ModuleContract;

namespace Homeport.Modules.EInvoicing.Api.Tests.Integration.Controllers;

public sealed class ValidationControllerTests
{
    public sealed class Validate(EInvoicingBehindGateway module) : IClassFixture<EInvoicingBehindGateway>
    {
        private const string ValidatePath = "/v1/validate";
        private const string ProblemPrefix = "https://homeport.data-worx.de/modules/e-invoicing/problems/";

        private const string UblInvoice = """<?xml version="1.0" encoding="UTF-8"?><Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"><ID>1</ID></Invoice>""";
        private const string UblCreditNote = """<?xml version="1.0" encoding="UTF-8"?><CreditNote xmlns="urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2"><ID>1</ID></CreditNote>""";
        private const string CiiInvoice = """<?xml version="1.0" encoding="UTF-8"?><rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100"/>""";
        private const string WithDoctype = """<?xml version="1.0"?><!DOCTYPE Invoice [<!ENTITY x "x">]><Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2">&x;</Invoice>""";
        private const string UnknownRoot = """<Order xmlns="urn:example:order"/>""";
        private const string NotXml = "Invoice 1, 100 EUR";

        [Theory]
        [InlineData(UblInvoice, "ubl-invoice")]
        [InlineData(UblCreditNote, "ubl-credit-note")]
        [InlineData(CiiInvoice, "cii")]
        public async Task Reports_The_Detected_Syntax_Of_An_Xml_Invoice(string document, string syntax)
        {
            var response = await PostAsync(document, "application/xml");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($$"""{"container":"xml","syntax":"{{syntax}}"}""", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Accepts_Text_Xml()
        {
            var response = await PostAsync(UblInvoice, "text/xml");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Reports_A_Pdf_Without_Syntax()
        {
            var response = await PostAsync("%PDF-1.7\n", "application/pdf");

            Assert.Equal("""{"container":"pdf","syntax":null}""", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Reports_One_Document_For_A_Processed_Document()
        {
            var response = await PostAsync(UblInvoice, "application/xml");

            Assert.Equal(["documents=1"], response.Headers.GetValues(ModuleHeaders.Usage));
        }

        [Theory]
        [InlineData(WithDoctype, "application/xml", "unreadable-document")]
        [InlineData(NotXml, "application/xml", "unreadable-document")]
        [InlineData(UblInvoice, "application/pdf", "unreadable-document")]
        [InlineData(UnknownRoot, "application/xml", "unknown-syntax")]
        public async Task Answers_A_Rejected_Document_With_Its_Problem(string document, string contentType, string problem)
        {
            var response = await PostAsync(document, contentType);

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(ProblemPrefix + problem, root.GetProperty("type").GetString());
        }

        [Theory]
        [InlineData(WithDoctype)]
        [InlineData(UnknownRoot)]
        public async Task Reports_No_Document_For_A_Rejected_Document(string document)
        {
            var response = await PostAsync(document, "application/xml");

            Assert.Equal(["documents=0"], response.Headers.GetValues(ModuleHeaders.Usage));
        }

        [Fact]
        public async Task Answers_Text_Plain_As_An_Unsupported_Media_Type_Problem()
        {
            var response = await PostAsync(UblInvoice, "text/plain");

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Answers_A_Caller_Without_The_Validate_Scope_As_A_Forbidden_Problem()
        {
            using var content = new StringContent(UblInvoice, Encoding.UTF8, "application/xml");

            var response = await module.CreateClient(scopes: "echo.invoke").PostAsync(ValidatePath, content);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        private async Task<HttpResponseMessage> PostAsync(string document, string contentType)
        {
            using var content = new StringContent(document, Encoding.UTF8, contentType);

            return await module.CreateClient().PostAsync(ValidatePath, content);
        }
    }
}
