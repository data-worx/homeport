// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections;
using System.Reflection;
using System.Text.Json;
using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Model;
using Homeport.Modules.EInvoicing.Domain.Reading;
using Homeport.Testing.EInvoicing;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Reading;

public sealed class InvoiceReaderTests
{
    public sealed class Read
    {
        [Theory]
        [InlineData(SampleInvoices.MaximalUbl, InvoiceSyntax.UblInvoice, SampleInvoices.MaximalCii)]
        [InlineData(SampleInvoices.CreditNoteUbl, InvoiceSyntax.UblCreditNote, SampleInvoices.CreditNoteCii)]
        public void Reads_The_Same_Invoice_In_Ubl_And_In_Cii_Into_Equal_Models(string ubl, InvoiceSyntax ublSyntax, string cii)
        {
            var fromUbl = ReadSample(ubl, ublSyntax);
            var fromCii = ReadSample(cii, InvoiceSyntax.Cii);

            Assert.Equal(JsonSerializer.Serialize(fromCii), JsonSerializer.Serialize(fromUbl));
        }

        [Theory]
        [InlineData(SampleInvoices.MaximalUbl, InvoiceSyntax.UblInvoice)]
        [InlineData(SampleInvoices.MaximalCii, InvoiceSyntax.Cii)]
        public void Reads_Every_Business_Term_Of_An_Invoice_That_Carries_Them_All(string sample, InvoiceSyntax syntax)
        {
            var invoice = ReadSample(sample, syntax);

            Assert.Empty(TermsLeftEmpty(invoice));
        }

        [Theory]
        [InlineData(SampleInvoices.MaximalUbl, InvoiceSyntax.UblInvoice)]
        [InlineData(SampleInvoices.MaximalCii, InvoiceSyntax.Cii)]
        public void Keeps_An_Amount_As_The_Document_Writes_It(string sample, InvoiceSyntax syntax)
        {
            var invoice = ReadSample(sample, syntax);

            Assert.Equal("1190.0", invoice.DocumentTotals!.TotalWithVat);
        }

        [Fact]
        public void Takes_The_Subject_Code_A_Ubl_Note_Carries_In_Front_Of_Its_Text()
        {
            var invoice = ReadSample(SampleInvoices.MaximalUbl, InvoiceSyntax.UblInvoice);

            Assert.Equal(("AAI", "Goods remain our property until paid in full."), (invoice.Notes[0].SubjectCode, invoice.Notes[0].Note));
        }

        [Theory]
        [InlineData("5", "3")]
        [InlineData("29", "35")]
        [InlineData("72", "432")]
        public void Codes_The_Vat_Point_Date_Code_Of_Cii_As_The_Semantic_Model_Does(string cii, string semantic)
        {
            var document = SampleInvoices.Text(SampleInvoices.MaximalCii)
                .Replace("<ram:DueDateTypeCode>5</ram:DueDateTypeCode>", $"<ram:DueDateTypeCode>{cii}</ram:DueDateTypeCode>", StringComparison.Ordinal);

            var invoice = InvoiceReader.Read(XElement.Parse(document), InvoiceSyntax.Cii);

            Assert.Equal(semantic, invoice.VatPointDateCode);
        }

        [Fact]
        public void Keeps_A_Vat_Point_Date_Code_Of_Cii_It_Does_Not_Know_As_Written()
        {
            var document = SampleInvoices.Text(SampleInvoices.MaximalCii)
                .Replace("<ram:DueDateTypeCode>5</ram:DueDateTypeCode>", "<ram:DueDateTypeCode>99</ram:DueDateTypeCode>", StringComparison.Ordinal);

            var invoice = InvoiceReader.Read(XElement.Parse(document), InvoiceSyntax.Cii);

            Assert.Equal("99", invoice.VatPointDateCode);
        }

        [Fact]
        public void Leaves_A_Date_Empty_That_Is_Not_Written_In_The_Format_Of_Its_Syntax()
        {
            var document = SampleInvoices.Text(SampleInvoices.MaximalUbl)
                .Replace("<cbc:IssueDate>2026-11-02</cbc:IssueDate>", "<cbc:IssueDate>02.11.2026</cbc:IssueDate>", StringComparison.Ordinal);

            var invoice = InvoiceReader.Read(XElement.Parse(document), InvoiceSyntax.UblInvoice);

            Assert.Null(invoice.IssueDate);
        }

        [Fact]
        public void Reads_An_Invoice_That_Carries_Nothing_Into_An_Empty_Model()
        {
            var invoice = InvoiceReader.Read(new XElement(XName.Get("Invoice", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2")), InvoiceSyntax.UblInvoice);

            Assert.Equal((null, 0), (invoice.Number, invoice.Lines.Count));
        }

        private static Invoice ReadSample(string sample, InvoiceSyntax syntax) =>
            InvoiceReader.Read(XElement.Parse(SampleInvoices.Text(sample)), syntax);

        /// <summary>Every term and group the model left empty, looking into the first entry of each list.</summary>
        private static List<string> TermsLeftEmpty(object model)
        {
            var empty = new List<string>();
            foreach (var property in model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<BusinessTermAttribute>() is not { } term)
                {
                    continue;
                }

                var value = property.GetValue(model);
                var first = value is IList list ? (list.Count > 0 ? list[0] : null) : value;
                if (first is null)
                {
                    empty.Add(term.Id);
                }
                else if (term.Id.StartsWith("BG-", StringComparison.Ordinal))
                {
                    empty.AddRange(TermsLeftEmpty(first));
                }
            }

            return empty;
        }
    }
}
