// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CoreRulesTests
{
    private const string UblPayee = "<cac:PayeeParty><cac:PartyIdentification><cbc:ID>P-1</cbc:ID></cac:PartyIdentification><cac:PartyName><cbc:Name>Payee</cbc:Name></cac:PartyName></cac:PayeeParty>";
    private const string CiiPayee = "<ram:PayeeTradeParty><ram:ID>P-1</ram:ID><ram:Name>Payee</ram:Name></ram:PayeeTradeParty>";
    private const string UblRepresentative = "<cac:TaxRepresentativeParty><cac:PartyName><cbc:Name>Representative</cbc:Name></cac:PartyName><cac:PostalAddress><cac:Country><cbc:IdentificationCode>DE</cbc:IdentificationCode></cac:Country></cac:PostalAddress><cac:PartyTaxScheme><cbc:CompanyID>DE123456789</cbc:CompanyID><cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme></cac:PartyTaxScheme></cac:TaxRepresentativeParty>";
    private const string CiiRepresentative = "<ram:SellerTaxRepresentativeTradeParty><ram:Name>Representative</ram:Name><ram:PostalTradeAddress><ram:CountryID>DE</ram:CountryID></ram:PostalTradeAddress><ram:SpecifiedTaxRegistration><ram:ID schemeID=\"VA\">DE123456789</ram:ID></ram:SpecifiedTaxRegistration></ram:SellerTaxRepresentativeTradeParty>";

    public sealed class Br06
    {
        [Fact]
        public void Holds_With_The_Seller_Name() => Assert.Empty(CoreRules.Br06(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_Without_The_Seller_Name() =>
            Assert.Equal(["$.seller.name"], CoreRules.Br06(RuleDocuments.Cii().Without("SellerTradeParty/Name").Input()));
    }

    public sealed class Br07
    {
        [Fact]
        public void Holds_With_The_Buyer_Name() => Assert.Empty(CoreRules.Br07(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_The_Buyer_Name() =>
            Assert.Equal(["$.buyer.name"], CoreRules.Br07(RuleDocuments.Ubl().Without("AccountingCustomerParty/Party/PartyLegalEntity").Input()));
    }

    public sealed class Br08
    {
        [Fact]
        public void Holds_With_The_Seller_Address() => Assert.Empty(CoreRules.Br08(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_The_Seller_Address() =>
            Assert.Equal(["$.seller.postalAddress"], CoreRules.Br08(RuleDocuments.Ubl().Without("AccountingSupplierParty/Party/PostalAddress").Input()));
    }

    public sealed class Br09
    {
        [Fact]
        public void Holds_With_The_Seller_Country() => Assert.Empty(CoreRules.Br09(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Holds_Without_A_Ubl_Seller_Address() =>
            Assert.Empty(CoreRules.Br09(RuleDocuments.Ubl().Without("AccountingSupplierParty/Party/PostalAddress").Input()));

        [Fact]
        public void Breaks_Without_The_Ubl_Seller_Country() =>
            Assert.Equal(["$.seller.postalAddress.countryCode"], CoreRules.Br09(RuleDocuments.Ubl().Without("AccountingSupplierParty/Party/PostalAddress/Country").Input()));

        [Fact]
        public void Breaks_Without_A_Cii_Seller_Address() =>
            Assert.Equal(["$.seller.postalAddress.countryCode"], CoreRules.Br09(RuleDocuments.Cii().Without("SellerTradeParty/PostalTradeAddress").Input()));
    }

    public sealed class Br10
    {
        [Fact]
        public void Holds_With_The_Buyer_Address() => Assert.Empty(CoreRules.Br10(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_Without_The_Buyer_Address() =>
            Assert.Equal(["$.buyer.postalAddress"], CoreRules.Br10(RuleDocuments.Cii().Without("BuyerTradeParty/PostalTradeAddress").Input()));
    }

    public sealed class Br11
    {
        [Fact]
        public void Holds_With_The_Buyer_Country() => Assert.Empty(CoreRules.Br11(RuleDocuments.Cii().Input()));

        [Fact]
        public void Holds_Without_A_Ubl_Buyer_Address() =>
            Assert.Empty(CoreRules.Br11(RuleDocuments.Ubl().Without("AccountingCustomerParty/Party/PostalAddress").Input()));

        [Fact]
        public void Breaks_Without_The_Ubl_Buyer_Country() =>
            Assert.Equal(["$.buyer.postalAddress.countryCode"], CoreRules.Br11(RuleDocuments.Ubl().Without("AccountingCustomerParty/Party/PostalAddress/Country").Input()));

        [Fact]
        public void Breaks_Without_A_Cii_Buyer_Address() =>
            Assert.Equal(["$.buyer.postalAddress.countryCode"], CoreRules.Br11(RuleDocuments.Cii().Without("BuyerTradeParty/PostalTradeAddress").Input()));
    }

    public sealed class Br17
    {
        [Fact]
        public void Holds_For_A_Ubl_Payee_That_Differs_From_The_Seller() =>
            Assert.Empty(CoreRules.Br17(RuleDocuments.Ubl().With("Invoice", UblPayee).Input()));

        [Fact]
        public void Holds_For_A_Cii_Payee_That_Differs_From_The_Seller() =>
            Assert.Empty(CoreRules.Br17(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiPayee).Input()));

        [Fact]
        public void Breaks_For_A_Payee_Without_A_Name() =>
            Assert.Equal(["$.payee"], CoreRules.Br17(RuleDocuments.Ubl().With("Invoice", UblPayee).Without("PayeeParty/PartyName").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Payee_Sharing_The_Sellers_Creditor_Identifier() =>
            Assert.Equal(["$.payee"], CoreRules.Br17(RuleDocuments.Ubl()
                .With("Invoice", UblPayee)
                .With("PayeeParty", "<cac:PartyIdentification><cbc:ID schemeID=\"SEPA\">DE98ZZZ09999999999</cbc:ID></cac:PartyIdentification>")
                .With("AccountingSupplierParty/Party", "<cac:PartyIdentification><cbc:ID schemeID=\"SEPA\">DE98ZZZ09999999999</cbc:ID></cac:PartyIdentification>")
                .Input()));

        [Fact]
        public void Breaks_For_A_Cii_Payee_Named_Like_The_Seller() =>
            Assert.Equal(["$.payee"], CoreRules.Br17(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiPayee).Set("PayeeTradeParty/Name", "Seller").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Payee_Sharing_The_Sellers_Legal_Registration() =>
            Assert.Equal(["$.payee"], CoreRules.Br17(RuleDocuments.Cii()
                .With("ApplicableHeaderTradeSettlement", CiiPayee)
                .With("PayeeTradeParty", "<ram:SpecifiedLegalOrganization><ram:ID>HRB 1</ram:ID></ram:SpecifiedLegalOrganization>")
                .With("SellerTradeParty", "<ram:SpecifiedLegalOrganization><ram:ID>HRB 1</ram:ID></ram:SpecifiedLegalOrganization>")
                .Input()));
    }

    public sealed class Br18
    {
        [Fact]
        public void Holds_With_The_Representatives_Name() =>
            Assert.Empty(CoreRules.Br18(RuleDocuments.Ubl().With("Invoice", UblRepresentative).Input()));

        [Fact]
        public void Breaks_Without_The_Representatives_Name() =>
            Assert.Equal(["$.sellerTaxRepresentative.name"], CoreRules.Br18(RuleDocuments.Cii().With("ApplicableHeaderTradeAgreement", CiiRepresentative).Without("SellerTaxRepresentativeTradeParty/Name").Input()));
    }

    public sealed class Br19
    {
        [Fact]
        public void Holds_With_The_Representatives_Address() =>
            Assert.Empty(CoreRules.Br19(RuleDocuments.Cii().With("ApplicableHeaderTradeAgreement", CiiRepresentative).Input()));

        [Fact]
        public void Breaks_Without_The_Representatives_Address() =>
            Assert.Equal(["$.sellerTaxRepresentative.postalAddress"], CoreRules.Br19(RuleDocuments.Ubl().With("Invoice", UblRepresentative).Without("TaxRepresentativeParty/PostalAddress").Input()));
    }

    public sealed class Br20
    {
        [Fact]
        public void Holds_With_The_Representatives_Country() =>
            Assert.Empty(CoreRules.Br20(RuleDocuments.Ubl().With("Invoice", UblRepresentative).Input()));

        [Fact]
        public void Holds_Without_A_Ubl_Representatives_Address() =>
            Assert.Empty(CoreRules.Br20(RuleDocuments.Ubl().With("Invoice", UblRepresentative).Without("TaxRepresentativeParty/PostalAddress").Input()));

        [Fact]
        public void Breaks_Without_The_Ubl_Representatives_Country() =>
            Assert.Equal(["$.sellerTaxRepresentative.postalAddress.countryCode"], CoreRules.Br20(RuleDocuments.Ubl().With("Invoice", UblRepresentative).Without("TaxRepresentativeParty/PostalAddress/Country").Input()));

        [Fact]
        public void Breaks_Without_A_Cii_Representatives_Address() =>
            Assert.Equal(["$.sellerTaxRepresentative.postalAddress.countryCode"], CoreRules.Br20(RuleDocuments.Cii().With("ApplicableHeaderTradeAgreement", CiiRepresentative).Without("SellerTaxRepresentativeTradeParty/PostalTradeAddress").Input()));
    }

    public sealed class Br56
    {
        [Fact]
        public void Holds_With_The_Ubl_Representatives_Vat_Identifier() =>
            Assert.Empty(CoreRules.Br56(RuleDocuments.Ubl().With("Invoice", UblRepresentative).Input()));

        [Fact]
        public void Holds_With_The_Cii_Representatives_Vat_Identifier() =>
            Assert.Empty(CoreRules.Br56(RuleDocuments.Cii().With("ApplicableHeaderTradeAgreement", CiiRepresentative).Input()));

        [Fact]
        public void Breaks_When_The_Ubl_Identifier_Is_Under_Another_Tax_Scheme() =>
            Assert.Equal(["$.sellerTaxRepresentative.vatIdentifier"], CoreRules.Br56(RuleDocuments.Ubl().With("Invoice", UblRepresentative).Set("TaxRepresentativeParty/PartyTaxScheme/TaxScheme/ID", "FC").Input()));

        [Fact]
        public void Breaks_When_The_Cii_Registration_Is_In_Another_Scheme() =>
            Assert.Equal(["$.sellerTaxRepresentative.vatIdentifier"], CoreRules.Br56(RuleDocuments.Cii().With("ApplicableHeaderTradeAgreement", CiiRepresentative.Replace("schemeID=\"VA\"", "schemeID=\"FC\"", StringComparison.Ordinal)).Input()));
    }

    public sealed class Br62
    {
        [Fact]
        public void Holds_For_A_Ubl_Endpoint_With_A_Scheme() =>
            Assert.Empty(CoreRules.Br62(RuleDocuments.Ubl().With("AccountingSupplierParty/Party", "<cbc:EndpointID schemeID=\"EM\">seller@example.com</cbc:EndpointID>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Endpoint_Without_A_Scheme() =>
            Assert.Equal(["$.seller.electronicAddress"], CoreRules.Br62(RuleDocuments.Ubl().With("AccountingSupplierParty/Party", "<cbc:EndpointID>seller@example.com</cbc:EndpointID>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Communication_With_A_Blank_Scheme() =>
            Assert.Equal(["$.seller.electronicAddress"], CoreRules.Br62(RuleDocuments.Cii().With("SellerTradeParty", "<ram:URIUniversalCommunication><ram:URIID schemeID=\" \">seller@example.com</ram:URIID></ram:URIUniversalCommunication>").Input()));
    }

    public sealed class Br63
    {
        [Fact]
        public void Holds_For_A_Cii_Communication_With_A_Scheme() =>
            Assert.Empty(CoreRules.Br63(RuleDocuments.Cii().With("BuyerTradeParty", "<ram:URIUniversalCommunication><ram:URIID schemeID=\"EM\">buyer@example.com</ram:URIID></ram:URIUniversalCommunication>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Communication_Without_An_Address() =>
            Assert.Equal(["$.buyer.electronicAddress"], CoreRules.Br63(RuleDocuments.Cii().With("BuyerTradeParty", "<ram:URIUniversalCommunication/>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Endpoint_Without_A_Scheme() =>
            Assert.Equal(["$.buyer.electronicAddress"], CoreRules.Br63(RuleDocuments.Ubl().With("AccountingCustomerParty/Party", "<cbc:EndpointID>buyer@example.com</cbc:EndpointID>").Input()));
    }
}
