// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.Schemas;
using Homeport.Testing.EInvoicing;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Schemas;

public sealed class SchemaRulesTests
{
    private static readonly SchemaCatalog Catalog = SchemaCatalog.LoadEmbedded();

    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Rsm = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
    private static readonly XNamespace Ram = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";

    public sealed class Validate
    {
        [Theory]
        [InlineData(SampleInvoices.MaximalUbl)]
        [InlineData(SampleInvoices.MaximalCii)]
        [InlineData(SampleInvoices.CreditNoteUbl)]
        [InlineData(SampleInvoices.CreditNoteCii)]
        public void Finds_Nothing_In_An_Invoice_That_Fits_Its_Schema(string sample)
        {
            var findings = SchemaRules.Validate(Sample(sample), Catalog);

            Assert.Empty(findings);
        }

        [Fact]
        public void Finds_Nothing_In_The_Smallest_Invoices_The_Tests_Use()
        {
            var findings = SchemaRules.Validate(XElement.Parse(MinimalInvoices.Ubl("x", "2026-11-24")), Catalog)
                .Concat(SchemaRules.Validate(XElement.Parse(MinimalInvoices.Cii("x", "20261124")), Catalog));

            Assert.Empty(findings);
        }

        [Fact]
        public void Names_The_Missing_Mandatory_Element_Of_A_Ubl_Invoice()
        {
            var root = Sample(SampleInvoices.MaximalUbl);
            root.Element(Cbc + "IssueDate")!.Remove();

            var finding = Assert.Single(SchemaRules.Validate(root, Catalog));

            Assert.Equal(
                new Finding(
                    FindingSeverity.Fatal,
                    SchemaRules.SchemaRule,
                    "The element 'cbc:DueDate' stands where the mandatory element 'cbc:IssueDate' is expected.",
                    "/Invoice/cbc:DueDate[1]"),
                finding);
        }

        [Fact]
        public void Names_The_Missing_Mandatory_Element_At_The_End_Of_A_Cii_Element()
        {
            var root = Sample(SampleInvoices.MaximalCii);
            root.Element(Rsm + "ExchangedDocument")!.Element(Ram + "IssueDateTime")!.Remove();
            root.Element(Rsm + "ExchangedDocument")!.Elements().Where(element => element.Name != Ram + "ID" && element.Name != Ram + "TypeCode").Remove();

            var finding = Assert.Single(SchemaRules.Validate(root, Catalog));

            Assert.Equal(
                ("The element 'rsm:ExchangedDocument' lacks the mandatory element 'ram:IssueDateTime'.", "/rsm:CrossIndustryInvoice/rsm:ExchangedDocument[1]"),
                (finding.Message, finding.Location));
        }

        [Fact]
        public void Names_An_Element_The_Schema_Does_Not_Know()
        {
            var root = Sample(SampleInvoices.MaximalUbl);
            root.Element(Cbc + "IssueDate")!.AddAfterSelf(new XElement(Cbc + "Unknown"));

            var finding = Assert.Single(SchemaRules.Validate(root, Catalog));

            Assert.Equal("/Invoice/cbc:Unknown[1]", finding.Location);
        }

        [Fact]
        public void Reports_A_Value_That_Does_Not_Fit_Its_Type()
        {
            var root = Sample(SampleInvoices.MaximalUbl);
            root.Element(Cbc + "IssueDate")!.Value = "02.11.2026";

            var finding = Assert.Single(SchemaRules.Validate(root, Catalog));

            Assert.Equal(
                ("The value of 'cbc:IssueDate' is not a valid date.", "/Invoice/cbc:IssueDate[1]"),
                (finding.Message, finding.Location));
        }

        [Fact]
        public void Names_The_Missing_Mandatory_Attribute()
        {
            var root = Sample(SampleInvoices.MaximalUbl);
            root.Descendants(Cbc + "PayableAmount").Single().Attribute("currencyID")!.Remove();

            var finding = Assert.Single(SchemaRules.Validate(root, Catalog));

            Assert.Equal("The element 'cbc:PayableAmount' lacks the mandatory attribute 'currencyID'.", finding.Message);
        }

        [Fact]
        public void Reports_An_Attribute_The_Schema_Does_Not_Declare()
        {
            var root = Sample(SampleInvoices.MaximalUbl);
            root.Element(Cbc + "IssueDate")!.SetAttributeValue("format", "102");

            var finding = Assert.Single(SchemaRules.Validate(root, Catalog));

            Assert.Equal(
                ("The element 'cbc:IssueDate' has no attribute 'format'.", "/Invoice/cbc:IssueDate[1]/@format"),
                (finding.Message, finding.Location));
        }

        [Fact]
        public void Reports_At_Most_The_Limit_And_Then_That_It_Stopped()
        {
            var root = Sample(SampleInvoices.MaximalUbl);
            var line = root.Element(Cac + "InvoiceLine")!;
            line.Element(Cbc + "InvoicedQuantity")!.Value = "many";
            line.AddAfterSelf(Enumerable.Range(0, SchemaRules.MaxFindings + 10).Select(_ => new XElement(line)));

            var findings = SchemaRules.Validate(root, Catalog);

            Assert.Equal(
                (SchemaRules.MaxFindings + 1, FindingSeverity.Information, SchemaRules.LimitRule),
                (findings.Count, findings[^1].Severity, findings[^1].Rule));
        }
    }

    private static XElement Sample(string name) => XDocument.Parse(SampleInvoices.Text(name)).Root!;
}
