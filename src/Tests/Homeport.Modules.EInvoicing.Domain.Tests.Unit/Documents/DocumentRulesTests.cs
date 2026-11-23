// SPDX-License-Identifier: AGPL-3.0-or-later

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
