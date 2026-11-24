// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Homeport.Shared.Client.ModuleContract;
using Homeport.Testing.EInvoicing;

namespace Homeport.Modules.EInvoicing.Api.Tests.Integration.Controllers;

public sealed class ReadingControllerTests
{
    public sealed class Read(EInvoicingBehindGateway module) : IClassFixture<EInvoicingBehindGateway>
    {
        private const string ReadPath = "/v1/read";
        private const string Zugferd1 = """<rsm:CrossIndustryDocument xmlns:rsm="urn:ferd:CrossIndustryDocument:invoice:1p0"/>""";

        [Fact]
        public async Task Returns_The_Report_And_The_Invoice_It_Read()
        {
            var response = await PostAsync(SampleInvoices.Text(SampleInvoices.MaximalUbl), "application/xml");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var root = await RootOf(response);
            Assert.Equal(
                ("ubl-invoice", "xrechnung", "INV-2026-0001"),
                (root.GetProperty("report").GetProperty("syntax").GetString(),
                 root.GetProperty("report").GetProperty("ruleSet").GetString(),
                 root.GetProperty("invoice").GetProperty("number").GetProperty("value").GetString()));
        }

        [Fact]
        public async Task Reports_The_Verdict_Of_The_Invoice_It_Read()
        {
            var response = await PostAsync(SampleInvoices.Text(SampleInvoices.MaximalCii), "application/xml");

            Assert.Equal("valid", (await RootOf(response)).GetProperty("report").GetProperty("verdict").GetString());
        }

        [Fact]
        public async Task Returns_An_Amount_As_The_Document_Writes_It()
        {
            var response = await PostAsync(SampleInvoices.Text(SampleInvoices.MaximalCii), "application/xml");

            var totals = (await RootOf(response)).GetProperty("invoice").GetProperty("documentTotals");
            Assert.Equal("\"1190.0\"", totals.GetProperty("totalWithVat").GetRawText());
        }

        [Fact]
        public async Task Returns_A_Date_In_Iso_8601()
        {
            var response = await PostAsync(SampleInvoices.Text(SampleInvoices.MaximalCii), "application/xml");

            Assert.Equal("2026-11-02", (await RootOf(response)).GetProperty("invoice").GetProperty("issueDate").GetString());
        }

        [Fact]
        public async Task Describes_An_Attachment_Without_Returning_Its_Content()
        {
            var response = await PostAsync(SampleInvoices.Text(SampleInvoices.MaximalUbl), "application/xml");

            var body = await response.Content.ReadAsStringAsync();
            var attachment = JsonDocument.Parse(body).RootElement.GetProperty("invoice").GetProperty("additionalSupportingDocuments")[0].GetProperty("attachedDocument");
            Assert.Equal("""{"mimeCode":"application/pdf","filename":"timesheet.pdf"}""", attachment.GetRawText());
            Assert.DoesNotContain("JVBERi0xLjQK", body, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Reads_The_Invoice_A_Pdf_Carries()
        {
            var response = await PostAsync(HybridPdf.Carrying("factur-x.xml", SampleInvoices.Text(SampleInvoices.MaximalCii)), "application/pdf");

            var root = await RootOf(response);
            Assert.Equal(
                ("pdf", "INV-2026-0001"),
                (root.GetProperty("report").GetProperty("container").GetString(), root.GetProperty("invoice").GetProperty("number").GetProperty("value").GetString()));
        }

        [Fact]
        public async Task Returns_No_Invoice_For_A_Zugferd_1_Document()
        {
            var response = await PostAsync(Zugferd1, "application/xml");

            Assert.Equal(JsonValueKind.Null, (await RootOf(response)).GetProperty("invoice").ValueKind);
        }

        [Fact]
        public async Task Reports_One_Document_For_A_Processed_Document()
        {
            var response = await PostAsync(SampleInvoices.Text(SampleInvoices.MaximalUbl), "application/xml");

            Assert.Equal(["documents=1"], response.Headers.GetValues(ModuleHeaders.Usage));
        }

        [Fact]
        public async Task Answers_A_Rejected_Document_With_Its_Problem_And_Reports_No_Document()
        {
            var response = await PostAsync("<Order/>", "application/xml");

            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal(["documents=0"], response.Headers.GetValues(ModuleHeaders.Usage));
            Assert.Equal(
                "https://homeport.data-worx.de/modules/e-invoicing/problems/unknown-syntax",
                (await RootOf(response)).GetProperty("type").GetString());
        }

        [Fact]
        public async Task Answers_Text_Plain_As_An_Unsupported_Media_Type_Problem()
        {
            var response = await PostAsync(SampleInvoices.Text(SampleInvoices.MaximalUbl), "text/plain");

            Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Answers_A_Caller_With_Only_The_Validate_Scope_As_A_Forbidden_Problem()
        {
            using var content = new StringContent(SampleInvoices.Text(SampleInvoices.MaximalUbl), Encoding.UTF8, "application/xml");

            var response = await module.CreateClient(EInvoicingBehindGateway.ValidateScope).PostAsync(ReadPath, content);

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        }

        private static async Task<JsonElement> RootOf(HttpResponseMessage response) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        private async Task<HttpResponseMessage> PostAsync(string document, string contentType)
        {
            using var content = new StringContent(document, Encoding.UTF8, contentType);

            return await module.CreateClient(EInvoicingBehindGateway.ReadScope).PostAsync(ReadPath, content);
        }

        private async Task<HttpResponseMessage> PostAsync(byte[] document, string contentType)
        {
            using var content = new ByteArrayContent(document);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            return await module.CreateClient(EInvoicingBehindGateway.ReadScope).PostAsync(ReadPath, content);
        }
    }
}
