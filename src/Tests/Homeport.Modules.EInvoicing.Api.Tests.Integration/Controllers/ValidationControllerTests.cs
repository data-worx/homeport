// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Homeport.Shared.Client.ModuleContract;
using Homeport.Testing.EInvoicing;

namespace Homeport.Modules.EInvoicing.Api.Tests.Integration.Controllers;

public sealed class ValidationControllerTests
{
    public sealed class Validate(EInvoicingBehindGateway module) : IClassFixture<EInvoicingBehindGateway>
    {
        private const string ValidatePath = "/v1/validate";
        private const string ProblemPrefix = "https://homeport.data-worx.de/modules/e-invoicing/problems/";

        private const string XRechnung = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0";
        // The smallest documents that fit the XML schemas and the business
        // rules, as MinimalInvoices writes them; spelled out here, since a
        // theory takes constants.
        private const string Namespaces = "xmlns:cac=\"urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2\" xmlns:cbc=\"urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2\"";
        private const string UblHeader = $"""<cbc:CustomizationID>{XRechnung}</cbc:CustomizationID><cbc:ID>1</cbc:ID><cbc:IssueDate>2026-11-23</cbc:IssueDate>""";
        private const string UblParties = """<cac:AccountingSupplierParty><cac:Party><cac:PostalAddress><cac:Country><cbc:IdentificationCode>DE</cbc:IdentificationCode></cac:Country></cac:PostalAddress><cac:PartyLegalEntity><cbc:RegistrationName>Seller</cbc:RegistrationName></cac:PartyLegalEntity></cac:Party></cac:AccountingSupplierParty><cac:AccountingCustomerParty><cac:Party><cac:PostalAddress><cac:Country><cbc:IdentificationCode>DE</cbc:IdentificationCode></cac:Country></cac:PostalAddress><cac:PartyLegalEntity><cbc:RegistrationName>Buyer</cbc:RegistrationName></cac:PartyLegalEntity></cac:Party></cac:AccountingCustomerParty><cac:LegalMonetaryTotal><cbc:LineExtensionAmount currencyID="EUR">0</cbc:LineExtensionAmount><cbc:TaxExclusiveAmount currencyID="EUR">0</cbc:TaxExclusiveAmount><cbc:TaxInclusiveAmount currencyID="EUR">0</cbc:TaxInclusiveAmount><cbc:PayableAmount currencyID="EUR">0</cbc:PayableAmount></cac:LegalMonetaryTotal>""";
        private const string UblLineContent = """<cbc:LineExtensionAmount currencyID="EUR">0</cbc:LineExtensionAmount><cac:Item><cbc:Name>Item</cbc:Name></cac:Item><cac:Price><cbc:PriceAmount currencyID="EUR">0</cbc:PriceAmount></cac:Price>""";
        private const string UblInvoice = $"""<?xml version="1.0" encoding="UTF-8"?><Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" {Namespaces}>{UblHeader}<cbc:InvoiceTypeCode>380</cbc:InvoiceTypeCode><cbc:DocumentCurrencyCode>EUR</cbc:DocumentCurrencyCode>{UblParties}<cac:InvoiceLine><cbc:ID>1</cbc:ID><cbc:InvoicedQuantity unitCode="C62">1</cbc:InvoicedQuantity>{UblLineContent}</cac:InvoiceLine></Invoice>""";
        private const string UblCreditNote = $"""<?xml version="1.0" encoding="UTF-8"?><CreditNote xmlns="urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2" {Namespaces}>{UblHeader}<cbc:CreditNoteTypeCode>381</cbc:CreditNoteTypeCode><cbc:DocumentCurrencyCode>EUR</cbc:DocumentCurrencyCode>{UblParties}<cac:CreditNoteLine><cbc:ID>1</cbc:ID><cbc:CreditedQuantity unitCode="C62">1</cbc:CreditedQuantity>{UblLineContent}</cac:CreditNoteLine></CreditNote>""";
        private const string WithoutIssueDate = $"""<?xml version="1.0" encoding="UTF-8"?><Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" {Namespaces}><cbc:CustomizationID>{XRechnung}</cbc:CustomizationID><cbc:ID>1</cbc:ID>{UblParties}<cac:InvoiceLine><cbc:ID>1</cbc:ID><cbc:LineExtensionAmount currencyID="EUR">0</cbc:LineExtensionAmount><cac:Item/></cac:InvoiceLine></Invoice>""";
        private const string CiiInvoice = $"""<?xml version="1.0" encoding="UTF-8"?><rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100" xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100" xmlns:udt="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100"><rsm:ExchangedDocumentContext><ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>{XRechnung}</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter></rsm:ExchangedDocumentContext><rsm:ExchangedDocument><ram:ID>1</ram:ID><ram:TypeCode>380</ram:TypeCode><ram:IssueDateTime><udt:DateTimeString format="102">20161123</udt:DateTimeString></ram:IssueDateTime></rsm:ExchangedDocument><rsm:SupplyChainTradeTransaction><ram:IncludedSupplyChainTradeLineItem><ram:AssociatedDocumentLineDocument><ram:LineID>1</ram:LineID></ram:AssociatedDocumentLineDocument><ram:SpecifiedTradeProduct><ram:Name>Item</ram:Name></ram:SpecifiedTradeProduct><ram:SpecifiedLineTradeAgreement><ram:NetPriceProductTradePrice><ram:ChargeAmount>0</ram:ChargeAmount></ram:NetPriceProductTradePrice></ram:SpecifiedLineTradeAgreement><ram:SpecifiedLineTradeDelivery><ram:BilledQuantity unitCode="C62">1</ram:BilledQuantity></ram:SpecifiedLineTradeDelivery><ram:SpecifiedLineTradeSettlement><ram:SpecifiedTradeSettlementLineMonetarySummation><ram:LineTotalAmount>0</ram:LineTotalAmount></ram:SpecifiedTradeSettlementLineMonetarySummation></ram:SpecifiedLineTradeSettlement></ram:IncludedSupplyChainTradeLineItem><ram:ApplicableHeaderTradeAgreement><ram:SellerTradeParty><ram:Name>Seller</ram:Name><ram:PostalTradeAddress><ram:CountryID>DE</ram:CountryID></ram:PostalTradeAddress></ram:SellerTradeParty><ram:BuyerTradeParty><ram:Name>Buyer</ram:Name><ram:PostalTradeAddress><ram:CountryID>DE</ram:CountryID></ram:PostalTradeAddress></ram:BuyerTradeParty></ram:ApplicableHeaderTradeAgreement><ram:ApplicableHeaderTradeDelivery/><ram:ApplicableHeaderTradeSettlement><ram:InvoiceCurrencyCode>EUR</ram:InvoiceCurrencyCode><ram:SpecifiedTradeSettlementHeaderMonetarySummation><ram:LineTotalAmount>0</ram:LineTotalAmount><ram:TaxBasisTotalAmount>0</ram:TaxBasisTotalAmount><ram:GrandTotalAmount>0</ram:GrandTotalAmount><ram:DuePayableAmount>0</ram:DuePayableAmount></ram:SpecifiedTradeSettlementHeaderMonetarySummation></ram:ApplicableHeaderTradeSettlement></rsm:SupplyChainTradeTransaction></rsm:CrossIndustryInvoice>""";
        private const string Minimum = """<rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100" xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100"><rsm:ExchangedDocumentContext><ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>urn:factur-x.eu:1p0:minimum</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter></rsm:ExchangedDocumentContext></rsm:CrossIndustryInvoice>""";
        private const string UnknownRuleSet = """<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"><cbc:CustomizationID>urn:fdc:peppol.eu:2017:poacc:billing:3.0</cbc:CustomizationID></Invoice>""";
        private const string Report = $"\"customizationId\":\"{XRechnung}\",\"ruleSet\":\"xrechnung\",\"ruleSetVersion\":\"3.0\",\"verdict\":\"valid\"";
        private const string WithDoctype = """<?xml version="1.0"?><!DOCTYPE Invoice [<!ENTITY x "x">]><Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2">&x;</Invoice>""";
        private const string UnknownRoot = """<Order xmlns="urn:example:order"/>""";
        private const string NotXml = "Invoice 1, 100 EUR";

        [Theory]
        [InlineData(UblInvoice, "ubl-invoice")]
        [InlineData(UblCreditNote, "ubl-credit-note")]
        public async Task Reports_The_Syntax_And_Rule_Set_Of_An_Xml_Invoice(string document, string syntax)
        {
            var response = await PostAsync(document, "application/xml");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal($$"""{"container":"xml","syntax":"{{syntax}}",{{Report}},"findings":[]}""", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Reports_A_Warning_For_An_Issue_Date_Outside_The_Window()
        {
            var response = await PostAsync(CiiInvoice, "application/xml");

            Assert.Equal(
                $$"""{"container":"xml","syntax":"cii",{{Report}},"findings":[{"severity":"warning","rule":"EINV-RULE-SET-WINDOW","message":"The invoice was issued on 2016-11-23, outside the window of version 3.0 (valid from 2023-08-01).","location":null,"path":null,"businessTerm":null}]}""",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Reports_Invalid_With_A_Fatal_Finding_Naming_The_Missing_Element()
        {
            var response = await PostAsync(WithoutIssueDate, "application/xml");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                $$"""{"container":"xml","syntax":"ubl-invoice","customizationId":"{{XRechnung}}","ruleSet":"xrechnung","ruleSetVersion":"3.0","verdict":"invalid","findings":[{"severity":"fatal","rule":"EINV-SCHEMA","message":"The element 'cac:AccountingSupplierParty' stands where the mandatory element 'cbc:IssueDate' is expected.","location":"/Invoice/cac:AccountingSupplierParty[1]","path":null,"businessTerm":null}]}""",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Reports_Invalid_With_A_Finding_For_A_Broken_Business_Rule()
        {
            var response = await PostAsync(UblInvoice.Replace("<cbc:Name>Item</cbc:Name>", string.Empty, StringComparison.Ordinal), "application/xml");

            Assert.Equal(
                $$"""{"container":"xml","syntax":"ubl-invoice","customizationId":"{{XRechnung}}","ruleSet":"xrechnung","ruleSetVersion":"3.0","verdict":"invalid","findings":[{"severity":"fatal","rule":"BR-25","message":"The invoice line carries no item name (BT-153).","location":null,"path":"$.lines[0].item.name","businessTerm":"BT-153"}]}""",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Reports_One_Document_For_An_Invalid_E_Invoice()
        {
            var response = await PostAsync(WithoutIssueDate, "application/xml");

            Assert.Equal(["documents=1"], response.Headers.GetValues(ModuleHeaders.Usage));
        }

        [Fact]
        public async Task Reports_The_Invoice_A_Pdf_Carries()
        {
            var response = await PostAsync(HybridPdf.Carrying("xrechnung.xml", UblInvoice, "XRECHNUNG"), "application/pdf");

            Assert.Equal($$"""{"container":"pdf","syntax":"ubl-invoice",{{Report}},"findings":[]}""", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Reports_Not_An_E_Invoice_For_A_Minimum_Profile()
        {
            var response = await PostAsync(Minimum, "application/xml");

            Assert.Equal(
                """{"container":"xml","syntax":"cii","customizationId":"urn:factur-x.eu:1p0:minimum","ruleSet":null,"ruleSetVersion":null,"verdict":"not-an-e-invoice","findings":[]}""",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task Reports_One_Document_For_A_Document_That_Is_No_E_Invoice()
        {
            var response = await PostAsync(Minimum, "application/xml");

            Assert.Equal(["documents=1"], response.Headers.GetValues(ModuleHeaders.Usage));
        }

        [Fact]
        public async Task Accepts_Text_Xml()
        {
            var response = await PostAsync(UblInvoice, "text/xml");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
        [InlineData(UnknownRuleSet, "application/xml", "unknown-rule-set")]
        public async Task Answers_A_Rejected_Document_With_Its_Problem(string document, string contentType, string problem)
        {
            var response = await PostAsync(document, contentType);

            await AssertProblem(response, problem);
        }

        [Fact]
        public async Task Answers_A_Pdf_Without_An_Embedded_Invoice_With_Its_Problem()
        {
            var response = await PostAsync(HybridPdf.Carrying("invoice.xml", UblInvoice), "application/pdf");

            await AssertProblem(response, "pdf-without-invoice");
        }

        [Fact]
        public async Task Reports_No_Document_For_A_Pdf_Without_An_Embedded_Invoice()
        {
            var response = await PostAsync(HybridPdf.Carrying("invoice.xml", UblInvoice), "application/pdf");

            Assert.Equal(["documents=0"], response.Headers.GetValues(ModuleHeaders.Usage));
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

        private static async Task AssertProblem(HttpResponseMessage response, string problem)
        {
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var root = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
            Assert.Equal(ProblemPrefix + problem, root.GetProperty("type").GetString());
        }

        private async Task<HttpResponseMessage> PostAsync(string document, string contentType)
        {
            using var content = new StringContent(document, Encoding.UTF8, contentType);

            return await module.CreateClient().PostAsync(ValidatePath, content);
        }

        private async Task<HttpResponseMessage> PostAsync(byte[] document, string contentType)
        {
            using var content = new ByteArrayContent(document);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            return await module.CreateClient().PostAsync(ValidatePath, content);
        }
    }
}
