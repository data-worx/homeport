// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CodeListRulesTests
{
    public sealed class BrCl01
    {
        [Fact]
        public void Holds_For_A_Commercial_Invoice() => Assert.Empty(CodeListRules.BrCl01(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_A_Code_Outside_Untdid_1001() =>
            Assert.Equal(["$.typeCode"], CodeListRules.BrCl01(RuleDocuments.Cii().Set("ExchangedDocument/TypeCode", "999").Input()));

        [Fact]
        public void Breaks_For_A_Credit_Note_Code_On_A_Ubl_Invoice() =>
            Assert.Equal(["$.typeCode"], CodeListRules.BrCl01(RuleDocuments.Ubl().Set("InvoiceTypeCode", "381").Input()));

        [Fact]
        public void Holds_For_A_Credit_Note_Code_On_A_Ubl_Credit_Note() =>
            Assert.Empty(CodeListRules.BrCl01(RuleDocuments.UblCreditNote().Set("CreditNoteTypeCode", "381").Input()));

        [Fact]
        public void Breaks_For_An_Invoice_Code_On_A_Ubl_Credit_Note() =>
            Assert.Equal(["$.typeCode"], CodeListRules.BrCl01(RuleDocuments.UblCreditNote().Input()));

        [Fact]
        public void Holds_For_A_Credit_Note_Code_In_Cii() =>
            Assert.Empty(CodeListRules.BrCl01(RuleDocuments.Cii().Set("ExchangedDocument/TypeCode", "381").Input()));
    }

    public sealed class BrCl03
    {
        [Fact]
        public void Holds_For_Amounts_In_Euro() => Assert.Empty(CodeListRules.BrCl03(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_An_Unknown_Currency_Of_A_Document_Amount() =>
            Assert.Equal(["$"], CodeListRules.BrCl03(RuleDocuments.Ubl().SetAttribute("TaxTotal/TaxAmount", "currencyID", "ABC").Input()));

        [Fact]
        public void Breaks_For_An_Unknown_Currency_Of_A_Line_Amount() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl03(RuleDocuments.Ubl().SetAttribute("InvoiceLine/LineExtensionAmount", "currencyID", "ABC").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Amount_Without_A_Currency() =>
            Assert.Equal(["$"], CodeListRules.BrCl03(RuleDocuments.Ubl().SetAttribute("LegalMonetaryTotal/PayableAmount", "currencyID", null).Input()));

        [Fact]
        public void Breaks_For_An_Unknown_Currency_Of_The_Cii_Total_Vat_Amount() =>
            Assert.Equal(["$.documentTotals"], CodeListRules.BrCl03(RuleDocuments.Cii().SetAttribute("TaxTotalAmount", "currencyID", "ABC").Input()));

        [Fact]
        public void Holds_For_A_Cii_Total_Vat_Amount_Without_A_Currency() =>
            Assert.Empty(CodeListRules.BrCl03(RuleDocuments.Cii().SetAttribute("TaxTotalAmount", "currencyID", null).Input()));
    }

    public sealed class BrCl04
    {
        [Fact]
        public void Holds_For_Euro() => Assert.Empty(CodeListRules.BrCl04(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_For_A_Code_Outside_Iso_4217() =>
            Assert.Equal(["$.currencyCode"], CodeListRules.BrCl04(RuleDocuments.Ubl().Set("DocumentCurrencyCode", "EURO").Input()));

        [Fact]
        public void Breaks_For_A_Lower_Case_Code() =>
            Assert.Equal(["$.currencyCode"], CodeListRules.BrCl04(RuleDocuments.Cii().Set("InvoiceCurrencyCode", "eur").Input()));

        [Fact]
        public void Holds_For_A_Code_Surrounded_By_Whitespace() =>
            Assert.Empty(CodeListRules.BrCl04(RuleDocuments.Ubl().Set("DocumentCurrencyCode", " EUR ").Input()));
    }

    public sealed class BrCl05
    {
        [Fact]
        public void Holds_For_A_Code_Of_Iso_4217() =>
            Assert.Empty(CodeListRules.BrCl05(RuleDocuments.Ubl().With("Invoice", "<cbc:TaxCurrencyCode>CHF</cbc:TaxCurrencyCode>").Input()));

        [Fact]
        public void Breaks_For_A_Code_Outside_Iso_4217() =>
            Assert.Equal(["$.vatAccountingCurrencyCode"], CodeListRules.BrCl05(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:TaxCurrencyCode>ABC</ram:TaxCurrencyCode>").Input()));
    }

    public sealed class BrCl06
    {
        [Fact]
        public void Holds_For_A_Ubl_Code() =>
            Assert.Empty(CodeListRules.BrCl06(RuleDocuments.Ubl().With("Invoice", "<cac:InvoicePeriod><cbc:DescriptionCode>35</cbc:DescriptionCode></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Code_In_Ubl() =>
            Assert.Equal(["$.vatPointDateCode"], CodeListRules.BrCl06(RuleDocuments.Ubl().With("Invoice", "<cac:InvoicePeriod><cbc:DescriptionCode>5</cbc:DescriptionCode></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Holds_For_A_Cii_Code() =>
            Assert.Empty(CodeListRules.BrCl06(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement/ApplicableTradeTax", "<ram:DueDateTypeCode>5</ram:DueDateTypeCode>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Code_In_Cii() =>
            Assert.Equal(["$.vatPointDateCode"], CodeListRules.BrCl06(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement/ApplicableTradeTax", "<ram:DueDateTypeCode>35</ram:DueDateTypeCode>").Input()));
    }

    public sealed class BrCl07
    {
        private const string Reference = "<cac:AdditionalDocumentReference><cbc:ID schemeID=\"{0}\">1</cbc:ID><cbc:DocumentTypeCode>{1}</cbc:DocumentTypeCode></cac:AdditionalDocumentReference>";

        [Fact]
        public void Holds_For_A_Code_Of_Untdid_1153() =>
            Assert.Empty(CodeListRules.BrCl07(RuleDocuments.Ubl().With("Invoice", string.Format(Reference, "AAA", "130")).Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Invoiced_Object_Identifier_Of_An_Unknown_Scheme() =>
            Assert.Equal(["$.invoicedObjectIdentifier"], CodeListRules.BrCl07(RuleDocuments.Ubl().With("Invoice", string.Format(Reference, "ZZZZ", "130")).Input()));

        [Fact]
        public void Holds_For_A_Ubl_Reference_Of_Another_Type() =>
            Assert.Empty(CodeListRules.BrCl07(RuleDocuments.Ubl().With("Invoice", string.Format(Reference, "ZZZZ", "916")).Input()));

        [Fact]
        public void Breaks_For_An_Unknown_Cii_Reference_Type_Code() =>
            Assert.Equal(["$.invoicedObjectIdentifier"], CodeListRules.BrCl07(RuleDocuments.Cii().With("ApplicableHeaderTradeAgreement", "<ram:AdditionalReferencedDocument><ram:IssuerAssignedID>1</ram:IssuerAssignedID><ram:TypeCode>130</ram:TypeCode><ram:ReferenceTypeCode>ZZZZ</ram:ReferenceTypeCode></ram:AdditionalReferencedDocument>").Input()));
    }

    public sealed class BrCl08
    {
        [Fact]
        public void Holds_For_A_Ubl_Note_With_A_Known_Subject_Code() =>
            Assert.Empty(CodeListRules.BrCl08(RuleDocuments.Ubl().With("Invoice", "<cbc:Note>#AAI#Text</cbc:Note>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Note_With_An_Unknown_Subject_Code() =>
            Assert.Equal(["$.notes[1]"], CodeListRules.BrCl08(RuleDocuments.Ubl().With("Invoice", "<cbc:Note>Text</cbc:Note><cbc:Note>#XYZ#Text</cbc:Note>").Input()));

        [Fact]
        public void Holds_For_A_Ubl_Note_Whose_Code_Is_Not_Three_Characters_Long() =>
            Assert.Empty(CodeListRules.BrCl08(RuleDocuments.Ubl().With("Invoice", "<cbc:Note>#ABCD#Text</cbc:Note>").Input()));

        [Fact]
        public void Holds_For_Three_Characters_The_List_Holds_Across_Two_Codes() =>
            Assert.Empty(CodeListRules.BrCl08(RuleDocuments.Ubl().With("Invoice", "<cbc:Note>#A A#Text</cbc:Note>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Note_With_An_Unknown_Subject_Code() =>
            Assert.Equal(["$.notes[0]"], CodeListRules.BrCl08(RuleDocuments.Cii().With("ExchangedDocument", "<ram:IncludedNote><ram:Content>Text</ram:Content><ram:SubjectCode>XYZ</ram:SubjectCode></ram:IncludedNote>").Input()));

        [Fact]
        public void Holds_For_A_Cii_Note_With_A_Known_Subject_Code() =>
            Assert.Empty(CodeListRules.BrCl08(RuleDocuments.Cii().With("ExchangedDocument", "<ram:IncludedNote><ram:Content>Text</ram:Content><ram:SubjectCode>AAI</ram:SubjectCode></ram:IncludedNote>").Input()));
    }

    public sealed class BrCl16
    {
        [Fact]
        public void Holds_For_A_Sepa_Credit_Transfer() =>
            Assert.Empty(CodeListRules.BrCl16(RuleDocuments.Ubl().With("Invoice", "<cac:PaymentMeans><cbc:PaymentMeansCode>58</cbc:PaymentMeansCode></cac:PaymentMeans>").Input()));

        [Fact]
        public void Breaks_For_A_Code_Outside_Untdid_4461() =>
            Assert.Equal(["$.paymentInstructions"], CodeListRules.BrCl16(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:SpecifiedTradeSettlementPaymentMeans><ram:TypeCode>99</ram:TypeCode></ram:SpecifiedTradeSettlementPaymentMeans>").Input()));
    }

    public sealed class BrCl22
    {
        [Fact]
        public void Holds_For_A_Code_Of_Vatex() =>
            Assert.Empty(CodeListRules.BrCl22(RuleDocuments.Ubl().With("TaxTotal/TaxSubtotal/TaxCategory", "<cbc:TaxExemptionReasonCode>VATEX-EU-132</cbc:TaxExemptionReasonCode>").Input()));

        [Fact]
        public void Breaks_For_A_Code_Outside_Vatex() =>
            Assert.Equal(["$"], CodeListRules.BrCl22(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement/ApplicableTradeTax", "<ram:ExemptionReasonCode>VATEX-XX</ram:ExemptionReasonCode>").Input()));
    }

    public sealed class BrCl24
    {
        private const string UblAttachment = "<cac:AdditionalDocumentReference><cbc:ID>1</cbc:ID><cac:Attachment><cbc:EmbeddedDocumentBinaryObject mimeCode=\"{0}\" filename=\"a\">AA==</cbc:EmbeddedDocumentBinaryObject></cac:Attachment></cac:AdditionalDocumentReference>";

        [Fact]
        public void Holds_For_A_Pdf() =>
            Assert.Empty(CodeListRules.BrCl24(RuleDocuments.Ubl().With("Invoice", string.Format(UblAttachment, "application/pdf")).Input()));

        [Fact]
        public void Breaks_For_Another_Media_Type() =>
            Assert.Equal(["$"], CodeListRules.BrCl24(RuleDocuments.Ubl().With("Invoice", string.Format(UblAttachment, "application/zip")).Input()));

        [Fact]
        public void Breaks_For_A_Media_Type_Surrounded_By_Whitespace() =>
            Assert.Equal(["$"], CodeListRules.BrCl24(RuleDocuments.Ubl().With("Invoice", string.Format(UblAttachment, " application/pdf")).Input()));

        [Fact]
        public void Breaks_For_Another_Media_Type_In_Cii() =>
            Assert.Equal(["$"], CodeListRules.BrCl24(RuleDocuments.Cii().With("ApplicableHeaderTradeAgreement", "<ram:AdditionalReferencedDocument><ram:IssuerAssignedID>1</ram:IssuerAssignedID><ram:TypeCode>916</ram:TypeCode><ram:AttachmentBinaryObject mimeCode=\"application/zip\" filename=\"a\">AA==</ram:AttachmentBinaryObject></ram:AdditionalReferencedDocument>").Input()));
    }
}
