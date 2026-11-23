// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Documents;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Documents;

public sealed class DocumentRulesTests
{
    public sealed class SyntaxOf
    {
        [Theory]
        [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", "Invoice", InvoiceSyntax.UblInvoice)]
        [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2", "CreditNote", InvoiceSyntax.UblCreditNote)]
        [InlineData("urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100", "CrossIndustryInvoice", InvoiceSyntax.Cii)]
        public void Returns_The_Syntax_The_Root_Element_Names(string namespaceUri, string localName, InvoiceSyntax expected)
        {
            var syntax = DocumentRules.SyntaxOf(namespaceUri, localName);

            Assert.Equal(expected, syntax.Value);
        }

        [Theory]
        [InlineData("urn:oasis:names:specification:ubl:schema:xsd:Invoice-2", "CreditNote")]
        [InlineData("urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2", "Invoice")]
        [InlineData("", "Invoice")]
        [InlineData("urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100", "crossindustryinvoice")]
        [InlineData("urn:example:order", "Order")]
        public void Returns_Unknown_Syntax_When_Namespace_And_Name_Do_Not_Match_A_Syntax(string namespaceUri, string localName)
        {
            var syntax = DocumentRules.SyntaxOf(namespaceUri, localName);

            Assert.Equal(DocumentRules.UnknownSyntaxCode, syntax.Error.Code);
        }
    }

    public sealed class IsZugferd1
    {
        [Fact]
        public void Returns_True_For_The_Root_Element_Of_Zugferd_1_0()
        {
            Assert.True(DocumentRules.IsZugferd1("urn:ferd:CrossIndustryDocument:invoice:1p0", "CrossIndustryDocument"));
        }

        [Fact]
        public void Returns_False_For_A_Cii_Invoice()
        {
            Assert.False(DocumentRules.IsZugferd1("urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100", "CrossIndustryInvoice"));
        }
    }

    public sealed class CustomizationIdOf
    {
        [Fact]
        public void Returns_The_Ubl_Customization_Id_Trimmed()
        {
            var root = XElement.Parse("""<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"><cbc:CustomizationID> urn:cen.eu:en16931:2017 </cbc:CustomizationID></Invoice>""");

            Assert.Equal("urn:cen.eu:en16931:2017", DocumentRules.CustomizationIdOf(root, InvoiceSyntax.UblInvoice));
        }

        [Fact]
        public void Returns_The_Cii_Guideline_Id()
        {
            var root = XElement.Parse("""<rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100" xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100"><rsm:ExchangedDocumentContext><ram:BusinessProcessSpecifiedDocumentContextParameter><ram:ID>urn:fdc:peppol.eu:2017:poacc:billing:01:1.0</ram:ID></ram:BusinessProcessSpecifiedDocumentContextParameter><ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>urn:factur-x.eu:1p0:minimum</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter></rsm:ExchangedDocumentContext></rsm:CrossIndustryInvoice>""");

            Assert.Equal("urn:factur-x.eu:1p0:minimum", DocumentRules.CustomizationIdOf(root, InvoiceSyntax.Cii));
        }

        [Fact]
        public void Returns_None_For_An_Empty_Customization_Id()
        {
            var root = XElement.Parse("""<Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"><cbc:CustomizationID> </cbc:CustomizationID></Invoice>""");

            Assert.Null(DocumentRules.CustomizationIdOf(root, InvoiceSyntax.UblInvoice));
        }
    }

    public sealed class IssueDateOf
    {
        [Fact]
        public void Returns_The_Ubl_Issue_Date()
        {
            var root = XElement.Parse("""<CreditNote xmlns="urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2" xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"><cbc:IssueDate>2026-11-23</cbc:IssueDate></CreditNote>""");

            Assert.Equal(new DateOnly(2026, 11, 23), DocumentRules.IssueDateOf(root, InvoiceSyntax.UblCreditNote));
        }

        [Fact]
        public void Returns_The_Cii_Issue_Date_In_Format_102()
        {
            Assert.Equal(new DateOnly(2016, 4, 4), DocumentRules.IssueDateOf(CiiWithDate("102", "20160404"), InvoiceSyntax.Cii));
        }

        [Fact]
        public void Returns_None_For_A_Cii_Issue_Date_In_Another_Format()
        {
            Assert.Null(DocumentRules.IssueDateOf(CiiWithDate("203", "201604041200"), InvoiceSyntax.Cii));
        }

        [Fact]
        public void Returns_None_For_A_Date_That_Is_Not_One()
        {
            Assert.Null(DocumentRules.IssueDateOf(CiiWithDate("102", "20161304"), InvoiceSyntax.Cii));
        }

        private static XElement CiiWithDate(string format, string date) => XElement.Parse($$"""<rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100" xmlns:ram="urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100" xmlns:udt="urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100"><rsm:ExchangedDocument><ram:IssueDateTime><udt:DateTimeString format="{{format}}">{{date}}</udt:DateTimeString></ram:IssueDateTime></rsm:ExchangedDocument></rsm:CrossIndustryInvoice>""");
    }

    public sealed class DeclaredProfileOf
    {
        [Fact]
        public void Returns_The_Factur_X_Conformance_Level_Written_As_An_Element()
        {
            var xmp = XDocument.Parse("""<x:xmpmeta xmlns:x="adobe:ns:meta/"><rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"><rdf:Description xmlns:fx="urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#"><fx:ConformanceLevel>EXTENDED</fx:ConformanceLevel></rdf:Description></rdf:RDF></x:xmpmeta>""");

            Assert.Equal("EXTENDED", DocumentRules.DeclaredProfileOf(xmp));
        }

        [Fact]
        public void Returns_The_Conformance_Level_Written_As_An_Attribute()
        {
            var xmp = XDocument.Parse("""<x:xmpmeta xmlns:x="adobe:ns:meta/"><rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"><rdf:Description xmlns:fx="urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#" fx:ConformanceLevel="BASIC"/></rdf:RDF></x:xmpmeta>""");

            Assert.Equal("BASIC", DocumentRules.DeclaredProfileOf(xmp));
        }

        [Fact]
        public void Returns_The_Conformance_Level_Of_The_Zugferd_2_0_Schema()
        {
            var xmp = XDocument.Parse("""<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"><rdf:Description xmlns:zf="urn:zugferd:pdfa:CrossIndustryDocument:invoice:1p0#"><zf:ConformanceLevel>EN 16931</zf:ConformanceLevel></rdf:Description></rdf:RDF>""");

            Assert.Equal("EN 16931", DocumentRules.DeclaredProfileOf(xmp));
        }

        [Fact]
        public void Returns_None_Without_A_Conformance_Level()
        {
            var xmp = XDocument.Parse("""<rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"><rdf:Description xmlns:pdf="http://ns.adobe.com/pdf/1.3/"><pdf:Producer>Example</pdf:Producer></rdf:Description></rdf:RDF>""");

            Assert.Null(DocumentRules.DeclaredProfileOf(xmp));
        }
    }

    public sealed class PdfWithoutInvoice
    {
        [Fact]
        public void Names_Every_File_Name_The_Module_Looks_For()
        {
            var error = DocumentRules.PdfWithoutInvoice();

            Assert.Equal(
                new(DocumentRules.PdfWithoutInvoiceCode, "The PDF carries no embedded file named 'factur-x.xml', 'xrechnung.xml', 'zugferd-invoice.xml', 'ZUGFeRD-invoice.xml'."),
                error);
        }
    }

    public sealed class Unreadable
    {
        [Fact]
        public void Returns_The_Unreadable_Error_With_The_Reason()
        {
            var error = DocumentRules.Unreadable("Not a PDF.");

            Assert.Equal(new(DocumentRules.UnreadableCode, "Not a PDF."), error);
        }
    }
}
