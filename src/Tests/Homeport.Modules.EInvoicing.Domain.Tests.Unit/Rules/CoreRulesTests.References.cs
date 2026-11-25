// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CoreRulesTests
{
    public sealed class Br52
    {
        [Fact]
        public void Holds_For_A_Supporting_Document_With_A_Reference() =>
            Assert.Empty(CoreRules.Br52(RuleDocuments.Ubl().With("Invoice", "<cac:AdditionalDocumentReference><cbc:ID>D-1</cbc:ID></cac:AdditionalDocumentReference>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Supporting_Document_Without_A_Reference() =>
            Assert.Equal(["$.additionalSupportingDocuments[1].reference"], CoreRules.Br52(RuleDocuments.Ubl().With("Invoice", "<cac:AdditionalDocumentReference><cbc:ID>O-1</cbc:ID><cbc:DocumentTypeCode>130</cbc:DocumentTypeCode></cac:AdditionalDocumentReference><cac:AdditionalDocumentReference><cbc:ID>D-1</cbc:ID></cac:AdditionalDocumentReference><cac:AdditionalDocumentReference><cbc:ID> </cbc:ID></cac:AdditionalDocumentReference>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Invoiced_Object_Without_A_Reference() =>
            Assert.Equal(["$.invoicedObjectIdentifier"], CoreRules.Br52(RuleDocuments.Ubl().With("Invoice", "<cac:AdditionalDocumentReference><cbc:ID> </cbc:ID><cbc:DocumentTypeCode>130</cbc:DocumentTypeCode></cac:AdditionalDocumentReference>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Line_Reference_Without_An_Identifier() =>
            Assert.Equal(["$.lines[0].objectIdentifier"], CoreRules.Br52(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", "<ram:AdditionalReferencedDocument><ram:TypeCode>130</ram:TypeCode></ram:AdditionalReferencedDocument>").Input()));
    }

    public sealed class Br55
    {
        [Fact]
        public void Holds_For_A_Preceding_Invoice_With_A_Number() =>
            Assert.Empty(CoreRules.Br55(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:InvoiceReferencedDocument><ram:IssuerAssignedID>R-0</ram:IssuerAssignedID></ram:InvoiceReferencedDocument>").Input()));

        [Fact]
        public void Holds_For_A_Ubl_Preceding_Invoice_With_A_Blank_Number() =>
            Assert.Empty(CoreRules.Br55(RuleDocuments.Ubl().With("Invoice", "<cac:BillingReference><cac:InvoiceDocumentReference><cbc:ID> </cbc:ID></cac:InvoiceDocumentReference></cac:BillingReference>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Billing_Reference_Without_An_Invoice() =>
            Assert.Equal(["$.precedingInvoiceReferences"], CoreRules.Br55(RuleDocuments.Ubl().With("Invoice", "<cac:BillingReference/>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Preceding_Invoice_With_A_Blank_Number() =>
            Assert.Equal(["$.precedingInvoiceReferences[0].reference"], CoreRules.Br55(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:InvoiceReferencedDocument><ram:IssuerAssignedID> </ram:IssuerAssignedID></ram:InvoiceReferencedDocument>").Input()));
    }

    public sealed class Br57
    {
        [Fact]
        public void Holds_For_A_Deliver_To_Address_With_A_Country() =>
            Assert.Empty(CoreRules.Br57(RuleDocuments.Ubl().With("Invoice", "<cac:Delivery><cac:DeliveryLocation><cac:Address><cac:Country><cbc:IdentificationCode>DE</cbc:IdentificationCode></cac:Country></cac:Address></cac:DeliveryLocation></cac:Delivery>").Input()));

        [Fact]
        public void Breaks_For_A_Second_Ubl_Delivery_Without_A_Country() =>
            Assert.Equal(["$.deliveryInformation.deliverToAddress.countryCode"], CoreRules.Br57(RuleDocuments.Ubl().With("Invoice", "<cac:Delivery><cac:DeliveryLocation><cac:Address><cac:Country><cbc:IdentificationCode>DE</cbc:IdentificationCode></cac:Country></cac:Address></cac:DeliveryLocation></cac:Delivery><cac:Delivery><cac:DeliveryLocation><cac:Address><cbc:CityName>Berlin</cbc:CityName></cac:Address></cac:DeliveryLocation></cac:Delivery>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Ship_To_Address_With_A_Blank_Country() =>
            Assert.Equal(["$.deliveryInformation.deliverToAddress.countryCode"], CoreRules.Br57(RuleDocuments.Cii().With("ApplicableHeaderTradeDelivery", "<ram:ShipToTradeParty><ram:PostalTradeAddress><ram:CountryID> </ram:CountryID></ram:PostalTradeAddress></ram:ShipToTradeParty>").Input()));
    }
}
