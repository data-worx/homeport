// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Homeport.Modules.EInvoicing.Application.Documents;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.Rules;
using Homeport.Modules.EInvoicing.Domain.RuleSets;
using Homeport.Modules.EInvoicing.Domain.Schemas;
using Homeport.Shared.Contracts;
using Homeport.Testing.EInvoicing;

namespace Homeport.Modules.EInvoicing.Application.Tests.Unit.Documents;

public sealed class DocumentInspectionTests
{
    public sealed class InspectAsync
    {
        private const string En16931 = "urn:cen.eu:en16931:2017";
        private const string XRechnung30 = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0";
        private const string XRechnung23 = "urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_2.3";
        private const string XRechnungExtension30 = XRechnung30 + "#conformant#urn:xeinkauf.de:kosit:extension:xrechnung_3.0";
        private const string XRechnungExtension23 = XRechnung23 + "#conformant#urn:xoev-de:kosit:extension:xrechnung_2.3";
        private const string Basic = "urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic";
        private const string Extended = "urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended";
        private const string Minimum = "urn:factur-x.eu:1p0:minimum";
        private const string BasicWl = "urn:factur-x.eu:1p0:basicwl";
        private const string Today = "2026-11-23";

        private const string Zugferd1 = """<rsm:CrossIndustryDocument xmlns:rsm="urn:ferd:CrossIndustryDocument:invoice:1p0" xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:12"><rsm:SpecifiedExchangedDocumentContext><ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>urn:ferd:CrossIndustryDocument:invoice:1p0:comfort</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter></rsm:SpecifiedExchangedDocumentContext></rsm:CrossIndustryDocument>""";

        private readonly DocumentInspection inspection = new(RuleSetCatalog.LoadEmbedded(), SchemaCatalog.LoadEmbedded(), RuleCatalog.LoadEmbedded());

        [Theory]
        [InlineData(En16931, RuleSet.En16931, "2017")]
        [InlineData(XRechnung30, RuleSet.XRechnung, "3.0")]
        [InlineData(XRechnungExtension30, RuleSet.XRechnungExtension, "3.0")]
        public async Task Detects_The_Rule_Set_Of_A_Ubl_Invoice(string customizationId, RuleSet ruleSet, string version)
        {
            var inspected = await Inspect(Ubl(customizationId, Today), DocumentContainer.Xml);

            Assert.Equal((InvoiceSyntax.UblInvoice, ruleSet, version), (inspected.Value.Syntax, inspected.Value.Match!.RuleSet!.RuleSet, inspected.Value.Match.RuleSet.Version));
            Assert.Empty(inspected.Value.Findings);
        }

        [Theory]
        [InlineData(En16931, RuleSet.En16931, "2017")]
        [InlineData(XRechnung30, RuleSet.XRechnung, "3.0")]
        [InlineData(XRechnungExtension30, RuleSet.XRechnungExtension, "3.0")]
        [InlineData(Basic, RuleSet.FacturXBasic, "1.0")]
        [InlineData(Extended, RuleSet.FacturXExtended, "1.0")]
        public async Task Detects_The_Rule_Set_Of_A_Cii_Invoice(string customizationId, RuleSet ruleSet, string version)
        {
            var inspected = await Inspect(Cii(customizationId, "20261123"), DocumentContainer.Xml);

            Assert.Equal((InvoiceSyntax.Cii, ruleSet, version), (inspected.Value.Syntax, inspected.Value.Match!.RuleSet!.RuleSet, inspected.Value.Match.RuleSet.Version));
            Assert.Empty(inspected.Value.Findings);
        }

        [Theory]
        [InlineData(XRechnung23, RuleSet.XRechnung)]
        [InlineData(XRechnungExtension23, RuleSet.XRechnungExtension)]
        public async Task Detects_The_Previous_Xrechnung_Version_Issued_Inside_Its_Window(string customizationId, RuleSet ruleSet)
        {
            var inspected = await Inspect(Ubl(customizationId, "2023-12-01"), DocumentContainer.Xml);

            Assert.Equal((ruleSet, "2.3"), (inspected.Value.Match!.RuleSet!.RuleSet, inspected.Value.Match.RuleSet.Version));
            Assert.Empty(inspected.Value.Findings);
        }

        [Fact]
        public async Task Detects_A_Ubl_Credit_Note()
        {
            var inspected = await Inspect(
                $"""<CreditNote xmlns="urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"><cbc:CustomizationID>{XRechnung30}</cbc:CustomizationID><cbc:IssueDate>{Today}</cbc:IssueDate></CreditNote>""",
                DocumentContainer.Xml);

            Assert.Equal((InvoiceSyntax.UblCreditNote, RuleSet.XRechnung), (inspected.Value.Syntax, inspected.Value.Match!.RuleSet!.RuleSet));
        }

        [Theory]
        [InlineData("factur-x.xml", Basic, "BASIC", RuleSet.FacturXBasic)]
        [InlineData("factur-x.xml", En16931, "EN 16931", RuleSet.En16931)]
        [InlineData("factur-x.xml", Extended, "EXTENDED", RuleSet.FacturXExtended)]
        [InlineData("xrechnung.xml", XRechnung30, "XRECHNUNG", RuleSet.XRechnung)]
        [InlineData("xrechnung.xml", XRechnungExtension30, "XRECHNUNG", RuleSet.XRechnungExtension)]
        public async Task Detects_The_Rule_Set_Of_The_Invoice_A_Pdf_Carries(string fileName, string customizationId, string profile, RuleSet ruleSet)
        {
            var pdf = HybridPdf.Carrying(fileName, Cii(customizationId, "20261123"), profile);

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.Equal((DocumentContainer.Pdf, InvoiceSyntax.Cii, ruleSet), (inspected.Value.Container, inspected.Value.Syntax, inspected.Value.Match!.RuleSet!.RuleSet));
            Assert.Empty(inspected.Value.Findings);
        }

        [Fact]
        public async Task Reads_A_Pdf_With_The_Zugferd_2_0_File_Name_And_Schema()
        {
            var pdf = HybridPdf.Carrying("zugferd-invoice.xml", Cii(En16931, "20261123"), "EN 16931", HybridPdf.ZugferdXmpNamespace);

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.Equal(RuleSet.En16931, inspected.Value.Match!.RuleSet!.RuleSet);
            Assert.Empty(inspected.Value.Findings);
        }

        [Fact]
        public async Task Reads_A_Pdf_Without_Xmp_Metadata_Without_A_Finding()
        {
            var pdf = HybridPdf.Carrying("factur-x.xml", Cii(Basic, "20261123"));

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.Equal(RuleSet.FacturXBasic, inspected.Value.Match!.RuleSet!.RuleSet);
            Assert.Empty(inspected.Value.Findings);
        }

        [Fact]
        public async Task Takes_The_Invoice_By_Its_Name_Among_Other_Attachments()
        {
            var pdf = HybridPdf.Create(
                [("timesheet.xml", Encoding.UTF8.GetBytes("<Order/>")), ("factur-x.xml", Encoding.UTF8.GetBytes(Cii(Extended, "20261123")))],
                xmp: null);

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.Equal(RuleSet.FacturXExtended, inspected.Value.Match!.RuleSet!.RuleSet);
        }

        [Fact]
        public async Task Warns_When_The_Xmp_Profile_Disagrees_With_The_Invoice()
        {
            var pdf = HybridPdf.Carrying("factur-x.xml", Cii(Extended, "20261123"), "BASIC");

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.Equal([RuleSetRules.ProfileRule], inspected.Value.Findings.Select(finding => finding.Rule));
        }

        [Theory]
        [InlineData(Minimum, "MINIMUM")]
        [InlineData(BasicWl, "BASIC WL")]
        public async Task Returns_Not_An_E_Invoice_For_Minimum_And_Basic_Wl_In_Xml(string customizationId, string profile)
        {
            var inspected = await Inspect(Cii(customizationId, "20261123"), DocumentContainer.Xml);

            Assert.Equal(Verdict.NotAnEInvoice, inspected.Value.Verdict);
            Assert.Equal(profile, inspected.Value.Match!.ExpectedProfile);
        }

        [Theory]
        [InlineData(Minimum, "MINIMUM")]
        [InlineData(BasicWl, "BASIC WL")]
        public async Task Returns_Not_An_E_Invoice_For_Minimum_And_Basic_Wl_In_A_Pdf(string customizationId, string profile)
        {
            var pdf = HybridPdf.Carrying("factur-x.xml", Cii(customizationId, "20261123"), profile);

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.Equal(Verdict.NotAnEInvoice, inspected.Value.Verdict);
            Assert.Empty(inspected.Value.Findings);
        }

        [Fact]
        public async Task Returns_Not_An_E_Invoice_For_Zugferd_1_0_In_Xml()
        {
            var inspected = await Inspect(Zugferd1, DocumentContainer.Xml);

            Assert.Equal(
                (DocumentContainer.Xml, (InvoiceSyntax?)null, (RuleSetMatch?)null, Verdict.NotAnEInvoice),
                (inspected.Value.Container, inspected.Value.Syntax, inspected.Value.Match, inspected.Value.Verdict));
        }

        [Fact]
        public async Task Returns_Not_An_E_Invoice_For_Zugferd_1_0_In_A_Pdf()
        {
            var pdf = HybridPdf.Carrying("ZUGFeRD-invoice.xml", Zugferd1);

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.Equal(Verdict.NotAnEInvoice, inspected.Value.Verdict);
        }

        [Fact]
        public async Task Returns_Valid_For_An_E_Invoice_That_Fits_Its_Schema()
        {
            var inspected = await Inspect(Ubl(XRechnung30, Today), DocumentContainer.Xml);

            Assert.Equal(Verdict.Valid, inspected.Value.Verdict);
        }

        [Fact]
        public async Task Returns_Invalid_With_A_Fatal_Finding_For_An_E_Invoice_That_Breaks_Its_Schema()
        {
            var broken = Cii(XRechnung30, "20261123").Replace("<ram:ApplicableHeaderTradeDelivery/>", string.Empty, StringComparison.Ordinal);

            var inspected = await Inspect(broken, DocumentContainer.Xml);

            Assert.Equal(
                (Verdict.Invalid, FindingSeverity.Fatal, SchemaRules.SchemaRule),
                (inspected.Value.Verdict, inspected.Value.Findings.Single().Severity, inspected.Value.Findings.Single().Rule));
        }

        [Fact]
        public async Task Validates_The_Invoice_A_Pdf_Carries()
        {
            var broken = Cii(XRechnung30, "20261123").Replace("<ram:ApplicableHeaderTradeDelivery/>", string.Empty, StringComparison.Ordinal);

            var inspected = await Inspect(HybridPdf.Carrying("factur-x.xml", broken), DocumentContainer.Pdf);

            Assert.Equal(Verdict.Invalid, inspected.Value.Verdict);
        }

        [Fact]
        public async Task Does_Not_Validate_A_Document_That_Is_No_E_Invoice()
        {
            var broken = Cii(Minimum, "20261123").Replace("<ram:ApplicableHeaderTradeDelivery/>", string.Empty, StringComparison.Ordinal);

            var inspected = await Inspect(broken, DocumentContainer.Xml);

            Assert.Equal((Verdict.NotAnEInvoice, 0), (inspected.Value.Verdict, inspected.Value.Findings.Count));
        }

        [Fact]
        public async Task Warns_When_The_Issue_Date_Lies_Outside_The_Window_Of_The_Version()
        {
            var inspected = await Inspect(Ubl(XRechnung23, Today), DocumentContainer.Xml);

            Assert.Equal([RuleSetRules.WindowRule], inspected.Value.Findings.Select(finding => finding.Rule));
        }

        [Fact]
        public async Task Checks_An_Unknown_Specification_Of_En16931_Against_En16931_With_A_Warning()
        {
            var inspected = await Inspect(Ubl(XRechnung30 + "#compliant#urn:xeinkauf.de:kosit:xrechnung:cvd_0.9", Today), DocumentContainer.Xml);

            Assert.Equal(RuleSet.En16931, inspected.Value.Match!.RuleSet!.RuleSet);
            Assert.Equal([RuleSetRules.UnknownSpecificationRule], inspected.Value.Findings.Select(finding => finding.Rule));
        }

        [Theory]
        [InlineData("urn:fdc:peppol.eu:2017:poacc:billing:3.0")]
        [InlineData("")]
        public async Task Returns_Unknown_Rule_Set_For_Any_Other_Specification(string customizationId)
        {
            var inspected = await Inspect(Ubl(customizationId, Today), DocumentContainer.Xml);

            Assert.Equal(RuleSetRules.UnknownRuleSetCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Pdf_Without_Invoice_For_A_Pdf_Carrying_No_Known_File_Name()
        {
            var pdf = HybridPdf.Carrying("invoice.xml", Cii(En16931, "20261123"), "EN 16931");

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.Equal(DocumentRules.PdfWithoutInvoiceCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Pdf_Without_Invoice_For_A_Pdf_Carrying_No_File()
        {
            var pdf = HybridPdf.Create([], xmp: null);

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.Equal(DocumentRules.PdfWithoutInvoiceCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Unreadable_For_A_Pdf_Whose_Invoice_Is_Not_Well_Formed()
        {
            var pdf = HybridPdf.Carrying("factur-x.xml", "<rsm:CrossIndustryInvoice");

            var inspected = await Inspect(pdf, DocumentContainer.Pdf);

            Assert.StartsWith("The invoice the PDF carries", inspected.Error.Message, StringComparison.Ordinal);
        }

        [Fact]
        public async Task Returns_Unreadable_For_A_Body_With_A_Pdf_Header_That_Is_No_Pdf()
        {
            var inspected = await Inspect(Encoding.ASCII.GetBytes("%PDF-1.7\n%âãÏÓ\n"), DocumentContainer.Pdf);

            Assert.Equal(DocumentRules.UnreadableCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Unreadable_For_Xml_With_A_Doctype()
        {
            var inspected = await Inspect(
                """<?xml version="1.0"?><!DOCTYPE Invoice [<!ENTITY x "x">]><Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2">&x;</Invoice>""",
                DocumentContainer.Xml);

            Assert.Equal(DocumentRules.UnreadableCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Unreadable_For_A_Body_That_Is_Not_Xml()
        {
            var inspected = await Inspect("Invoice 1, 100 EUR", DocumentContainer.Xml);

            Assert.Equal(DocumentRules.UnreadableCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Unreadable_For_An_Empty_Body()
        {
            var inspected = await Inspect(string.Empty, DocumentContainer.Xml);

            Assert.Equal(DocumentRules.UnreadableCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Unreadable_For_A_Known_Root_Element_That_Is_Not_Closed()
        {
            var inspected = await Inspect(
                """<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"><ID>1</ID>""",
                DocumentContainer.Xml);

            Assert.Equal(DocumentRules.UnreadableCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Unknown_Syntax_For_An_Unknown_Root_Element()
        {
            var inspected = await Inspect("""<Order xmlns="urn:example:order"/>""", DocumentContainer.Xml);

            Assert.Equal(DocumentRules.UnknownSyntaxCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Unreadable_For_A_Pdf_Without_A_Pdf_Header()
        {
            var inspected = await Inspect(Ubl(XRechnung30, Today), DocumentContainer.Pdf);

            Assert.Equal(DocumentRules.UnreadableCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Unreadable_For_A_Pdf_Shorter_Than_The_Header()
        {
            var inspected = await Inspect("%PD", DocumentContainer.Pdf);

            Assert.Equal(DocumentRules.UnreadableCode, inspected.Error.Code);
        }

        public static TheoryData<string> TestSuite => new(TestSuiteInvoices.Names.Except(KnownVerdictDifferences.All));

        public static TheoryData<string> KnownDifferences => new(KnownVerdictDifferences.All);

        // Upstream publishes every instance of the KoSIT test suite as a valid
        // XRechnung, so that is the verdict each one is expected to get.
        [Theory]
        [MemberData(nameof(TestSuite))]
        public async Task Judges_An_Instance_Of_The_KoSIT_Test_Suite_Valid(string name)
        {
            var inspected = await Inspect(TestSuiteInvoices.Bytes(name), DocumentContainer.Xml);

            Assert.Equal(Verdict.Valid, inspected.Value.Verdict);
        }

        [Theory]
        [MemberData(nameof(KnownDifferences))]
        public async Task Judges_An_Instance_Whose_Difference_From_KoSIT_Is_Known_Invalid(string name)
        {
            var inspected = await Inspect(TestSuiteInvoices.Bytes(name), DocumentContainer.Xml);

            Assert.Equal(Verdict.Invalid, inspected.Value.Verdict);
        }

        private static string Ubl(string customizationId, string issueDate) => MinimalInvoices.Ubl(customizationId, issueDate);

        private static string Cii(string customizationId, string issueDate) => MinimalInvoices.Cii(customizationId, issueDate);

        private Task<Result<InspectedDocument>> Inspect(string document, DocumentContainer container) =>
            Inspect(Encoding.UTF8.GetBytes(document), container);

        private async Task<Result<InspectedDocument>> Inspect(byte[] document, DocumentContainer container)
        {
            using var stream = new MemoryStream(document);

            return await inspection.InspectAsync(stream, container, CancellationToken.None);
        }
    }

    public sealed class ReadAsync
    {
        private const string XRechnung30 = "urn:cen.eu:en16931:2017#compliant#urn:xeinkauf.de:kosit:xrechnung_3.0";

        private const string Zugferd1 = """<rsm:CrossIndustryDocument xmlns:rsm="urn:ferd:CrossIndustryDocument:invoice:1p0"/>""";

        private readonly DocumentInspection inspection = new(RuleSetCatalog.LoadEmbedded(), SchemaCatalog.LoadEmbedded(), RuleCatalog.LoadEmbedded());

        [Theory]
        [InlineData(SampleInvoices.MaximalUbl)]
        [InlineData(SampleInvoices.MaximalCii)]
        public async Task Reads_The_Invoice_An_Xml_Document_Holds(string sample)
        {
            var read = await Read(Encoding.UTF8.GetBytes(SampleInvoices.Text(sample)), DocumentContainer.Xml);

            Assert.Equal("INV-2026-0001", read.Value.Invoice?.Number?.Value);
        }

        [Fact]
        public async Task Reads_The_Invoice_A_Pdf_Carries()
        {
            var read = await Read(HybridPdf.Carrying("factur-x.xml", SampleInvoices.Text(SampleInvoices.MaximalCii)), DocumentContainer.Pdf);

            Assert.Equal((DocumentContainer.Pdf, "INV-2026-0001"), (read.Value.Report.Container, read.Value.Invoice?.Number?.Value));
        }

        [Fact]
        public async Task Reports_What_Inspecting_The_Document_Reports()
        {
            var read = await Read(Encoding.UTF8.GetBytes(SampleInvoices.Text(SampleInvoices.MaximalUbl)), DocumentContainer.Xml);

            Assert.Equal((InvoiceSyntax.UblInvoice, RuleSet.XRechnung), (read.Value.Report.Syntax, read.Value.Report.Match?.RuleSet?.RuleSet));
        }

        [Fact]
        public async Task Validates_The_Invoice_It_Reads()
        {
            var read = await Read(Encoding.UTF8.GetBytes(MinimalInvoices.Cii(XRechnung30, "20261125")), DocumentContainer.Xml);

            Assert.Equal(Verdict.Valid, read.Value.Report.Verdict);
        }

        [Fact]
        public async Task Reads_No_Invoice_From_A_Zugferd_1_Document()
        {
            var read = await Read(Encoding.UTF8.GetBytes(Zugferd1), DocumentContainer.Xml);

            Assert.Equal((Verdict.NotAnEInvoice, null), (read.Value.Report.Verdict, read.Value.Invoice));
        }

        [Fact]
        public async Task Warns_That_The_Extension_Of_An_Extended_Invoice_Was_Ignored()
        {
            var extension = MinimalInvoices.Ubl($"{XRechnung30}#conformant#urn:xeinkauf.de:kosit:extension:xrechnung_3.0", "2026-11-25");

            var read = await Read(Encoding.UTF8.GetBytes(extension), DocumentContainer.Xml);

            Assert.Equal([RuleSetRules.ExtensionIgnoredRule], read.Value.Report.Findings.Select(finding => finding.Rule));
        }

        [Fact]
        public async Task Returns_The_Error_Of_A_Document_It_Cannot_Inspect()
        {
            var read = await Read(Encoding.UTF8.GetBytes("<Order/>"), DocumentContainer.Xml);

            Assert.Equal(DocumentRules.UnknownSyntaxCode, read.Error.Code);
        }

        private async Task<Result<ReadDocument>> Read(byte[] document, DocumentContainer container)
        {
            using var stream = new MemoryStream(document);

            return await inspection.ReadAsync(stream, container, CancellationToken.None);
        }
    }
}
