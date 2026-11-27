// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CodeListRulesTests
{
    public sealed class BrCl10
    {
        [Fact]
        public void Holds_For_A_Scheme_Of_Iso_6523() =>
            Assert.Empty(CodeListRules.BrCl10(RuleDocuments.Ubl().With("AccountingSupplierParty/Party", "<cac:PartyIdentification><cbc:ID schemeID=\"0088\">1</cbc:ID></cac:PartyIdentification>").Input()));

        [Fact]
        public void Breaks_For_A_Scheme_Outside_Iso_6523() =>
            Assert.Equal(["$.buyer"], CodeListRules.BrCl10(RuleDocuments.Ubl().With("AccountingCustomerParty/Party", "<cac:PartyIdentification><cbc:ID schemeID=\"0000\">1</cbc:ID></cac:PartyIdentification>").Input()));

        [Fact]
        public void Holds_For_A_Sepa_Creditor_Identifier_Of_The_Seller() =>
            Assert.Empty(CodeListRules.BrCl10(RuleDocuments.Ubl().With("AccountingSupplierParty/Party", "<cac:PartyIdentification><cbc:ID schemeID=\"SEPA\">DE98ZZZ09999999999</cbc:ID></cac:PartyIdentification>").Input()));

        [Fact]
        public void Breaks_For_A_Sepa_Creditor_Identifier_Of_The_Buyer() =>
            Assert.Equal(["$.buyer"], CodeListRules.BrCl10(RuleDocuments.Ubl().With("AccountingCustomerParty/Party", "<cac:PartyIdentification><cbc:ID schemeID=\"SEPA\">DE98ZZZ09999999999</cbc:ID></cac:PartyIdentification>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Global_Identifier_Of_An_Unknown_Scheme() =>
            Assert.Equal(["$.seller"], CodeListRules.BrCl10(RuleDocuments.Cii().With("SellerTradeParty", "<ram:GlobalID schemeID=\"0000\">1</ram:GlobalID>").Input()));

        [Fact]
        public void Leaves_A_Cii_Global_Identifier_Of_A_Product_To_BR_CL_21() =>
            Assert.Empty(CodeListRules.BrCl10(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:GlobalID schemeID=\"0000\">1</ram:GlobalID>").Input()));

        [Fact]
        public void Leaves_A_Cii_Global_Identifier_Of_The_Ship_To_Party_To_BR_CL_26() =>
            Assert.Empty(CodeListRules.BrCl10(RuleDocuments.Cii().With("ApplicableHeaderTradeDelivery", "<ram:ShipToTradeParty><ram:GlobalID schemeID=\"0000\">1</ram:GlobalID></ram:ShipToTradeParty>").Input()));
    }

    public sealed class BrCl11
    {
        [Fact]
        public void Holds_For_A_Scheme_Of_Iso_6523() =>
            Assert.Empty(CodeListRules.BrCl11(RuleDocuments.Ubl().With("AccountingSupplierParty/Party/PartyLegalEntity", "<cbc:CompanyID schemeID=\"0088\">1</cbc:CompanyID>").Input()));

        [Fact]
        public void Breaks_For_A_Scheme_Outside_Iso_6523() =>
            Assert.Equal(["$.seller"], CodeListRules.BrCl11(RuleDocuments.Ubl().With("AccountingSupplierParty/Party/PartyLegalEntity", "<cbc:CompanyID schemeID=\"0000\">1</cbc:CompanyID>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Legal_Organization_Of_An_Unknown_Scheme() =>
            Assert.Equal(["$.seller"], CodeListRules.BrCl11(RuleDocuments.Cii().With("SellerTradeParty", "<ram:SpecifiedLegalOrganization><ram:ID schemeID=\"0000\">1</ram:ID></ram:SpecifiedLegalOrganization>").Input()));

        [Fact]
        public void Holds_For_A_Cii_Tax_Registration() => Assert.Empty(CodeListRules.BrCl11(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_For_A_Cii_Origin_Country_With_A_Scheme() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl11(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:OriginTradeCountry><ram:ID schemeID=\"0000\">DE</ram:ID></ram:OriginTradeCountry>").Input()));
    }

    public sealed class BrCl14
    {
        [Fact]
        public void Holds_For_Germany() => Assert.Empty(CodeListRules.BrCl14(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_A_Code_Outside_Iso_3166() =>
            Assert.Equal(["$.buyer"], CodeListRules.BrCl14(RuleDocuments.Ubl().Set("AccountingCustomerParty/Party/PostalAddress/Country/IdentificationCode", "XX").Input()));

        [Fact]
        public void Breaks_For_The_Greek_Vat_Prefix_In_Cii() =>
            Assert.Equal(["$.seller"], CodeListRules.BrCl14(RuleDocuments.Cii().Set("SellerTradeParty/PostalTradeAddress/CountryID", "EL").Input()));
    }

    public sealed class BrCl25
    {
        [Fact]
        public void Holds_For_A_Scheme_Of_Eas() =>
            Assert.Empty(CodeListRules.BrCl25(RuleDocuments.Ubl().With("AccountingSupplierParty/Party", "<cbc:EndpointID schemeID=\"0088\">1</cbc:EndpointID>").Input()));

        [Fact]
        public void Breaks_For_A_Scheme_Outside_Eas() =>
            Assert.Equal(["$.seller"], CodeListRules.BrCl25(RuleDocuments.Ubl().With("AccountingSupplierParty/Party", "<cbc:EndpointID schemeID=\"0000\">1</cbc:EndpointID>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Address_Of_An_Unknown_Scheme() =>
            Assert.Equal(["$.buyer"], CodeListRules.BrCl25(RuleDocuments.Cii().With("BuyerTradeParty", "<ram:URIUniversalCommunication><ram:URIID schemeID=\"0000\">1</ram:URIID></ram:URIUniversalCommunication>").Input()));
    }

    public sealed class BrCl26
    {
        [Fact]
        public void Holds_For_A_Scheme_Of_Iso_6523() =>
            Assert.Empty(CodeListRules.BrCl26(RuleDocuments.Ubl().With("Invoice", "<cac:Delivery><cac:DeliveryLocation><cbc:ID schemeID=\"0088\">1</cbc:ID></cac:DeliveryLocation></cac:Delivery>").Input()));

        [Fact]
        public void Breaks_For_A_Scheme_Outside_Iso_6523() =>
            Assert.Equal(["$.deliveryInformation"], CodeListRules.BrCl26(RuleDocuments.Ubl().With("Invoice", "<cac:Delivery><cac:DeliveryLocation><cbc:ID schemeID=\"0000\">1</cbc:ID></cac:DeliveryLocation></cac:Delivery>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Ship_To_Party_Of_An_Unknown_Scheme() =>
            Assert.Equal(["$.deliveryInformation"], CodeListRules.BrCl26(RuleDocuments.Cii().With("ApplicableHeaderTradeDelivery", "<ram:ShipToTradeParty><ram:GlobalID schemeID=\"0000\">1</ram:GlobalID></ram:ShipToTradeParty>").Input()));
    }
}
