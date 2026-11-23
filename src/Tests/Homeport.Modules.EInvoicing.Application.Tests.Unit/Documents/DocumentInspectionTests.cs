// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text;
using Homeport.Modules.EInvoicing.Application.Documents;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Shared.Contracts;

namespace Homeport.Modules.EInvoicing.Application.Tests.Unit.Documents;

public sealed class DocumentInspectionTests
{
    public sealed class InspectAsync
    {
        private const string UblInvoice = """<?xml version="1.0" encoding="UTF-8"?><Invoice xmlns="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"><ID>1</ID></Invoice>""";

        private readonly DocumentInspection inspection = new();

        [Fact]
        public async Task Returns_The_Syntax_Of_A_Well_Formed_Xml_Invoice()
        {
            var inspected = await Inspect(UblInvoice, DocumentContainer.Xml);

            Assert.Equal(new InspectedDocument(DocumentContainer.Xml, InvoiceSyntax.UblInvoice), inspected.Value);
        }

        [Fact]
        public async Task Returns_The_Syntax_Of_A_Prefixed_Root_Element()
        {
            var inspected = await Inspect(
                """<rsm:CrossIndustryInvoice xmlns:rsm="urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100"/>""",
                DocumentContainer.Xml);

            Assert.Equal(InvoiceSyntax.Cii, inspected.Value.Syntax);
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
        public async Task Returns_A_Pdf_Without_Syntax_For_A_Body_With_A_Pdf_Header()
        {
            var inspected = await Inspect("%PDF-1.7\n%âãÏÓ\n", DocumentContainer.Pdf);

            Assert.Equal(new InspectedDocument(DocumentContainer.Pdf, Syntax: null), inspected.Value);
        }

        [Fact]
        public async Task Returns_Unreadable_For_A_Pdf_Without_A_Pdf_Header()
        {
            var inspected = await Inspect(UblInvoice, DocumentContainer.Pdf);

            Assert.Equal(DocumentRules.UnreadableCode, inspected.Error.Code);
        }

        [Fact]
        public async Task Returns_Unreadable_For_A_Pdf_Shorter_Than_The_Header()
        {
            var inspected = await Inspect("%PD", DocumentContainer.Pdf);

            Assert.Equal(DocumentRules.UnreadableCode, inspected.Error.Code);
        }

        private async Task<Result<InspectedDocument>> Inspect(string document, DocumentContainer container)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(document));

            return await inspection.InspectAsync(stream, container, CancellationToken.None);
        }
    }
}
