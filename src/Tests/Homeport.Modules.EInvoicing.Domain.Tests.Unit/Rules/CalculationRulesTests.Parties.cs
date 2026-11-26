// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CalculationRulesTests
{
    public sealed class BrCo09
    {
        [Fact]
        public void Holds_For_A_Known_Prefix() => Assert.Empty(CalculationRules.BrCo09(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_A_Prefix_The_List_Does_Not_Know() =>
            Assert.Equal(["$.seller.vatIdentifier"], CalculationRules.BrCo09(RuleDocuments.Ubl().Set("PartyTaxScheme/CompanyID", "EL123456789").Input()));

        [Fact]
        public void Breaks_For_A_Lower_Case_Prefix() =>
            Assert.Equal(["$.seller.vatIdentifier"], CalculationRules.BrCo09(RuleDocuments.Cii().Set("SpecifiedTaxRegistration/ID", "de123456789").Input()));

        [Fact]
        public void Breaks_For_A_Prefix_After_A_Space() =>
            Assert.Equal(["$.seller.vatIdentifier"], CalculationRules.BrCo09(RuleDocuments.Ubl().Set("PartyTaxScheme/CompanyID", " DE123456789").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Vat_Scheme_Without_An_Identifier() =>
            Assert.Equal(["$.seller.vatIdentifier"], CalculationRules.BrCo09(RuleDocuments.Ubl().Without("PartyTaxScheme/CompanyID").Input()));

        [Fact]
        public void Breaks_For_The_Buyer() =>
            Assert.Equal(["$.buyer.vatIdentifier"], CalculationRules.BrCo09(RuleDocuments.Ubl().With("AccountingCustomerParty/Party", "<cac:PartyTaxScheme><cbc:CompanyID>XX1</cbc:CompanyID><cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme></cac:PartyTaxScheme>").Input()));

        [Fact]
        public void Holds_For_A_Ubl_Scheme_Other_Than_Vat() =>
            Assert.Empty(CalculationRules.BrCo09(RuleDocuments.Ubl().Set("PartyTaxScheme/CompanyID", "XX1").Set("PartyTaxScheme/TaxScheme/ID", "GST").Input()));

        [Fact]
        public void Holds_For_A_Cii_Tax_Registration_Of_Another_Scheme() =>
            Assert.Empty(CalculationRules.BrCo09(RuleDocuments.Cii().With("SellerTradeParty", "<ram:SpecifiedTaxRegistration><ram:ID schemeID=\"FC\">201/113/40209</ram:ID></ram:SpecifiedTaxRegistration>").Input()));
    }

    public sealed class BrCo26
    {
        [Fact]
        public void Holds_For_A_Seller_With_A_Vat_Identifier() => Assert.Empty(CalculationRules.BrCo26(RuleDocuments.Cii().Input()));

        [Fact]
        public void Holds_For_A_Seller_With_A_Legal_Registration_Identifier() =>
            Assert.Empty(CalculationRules.BrCo26(RuleDocuments.Ubl().Without("PartyTaxScheme").With("PartyLegalEntity", "<cbc:CompanyID>HRB 1</cbc:CompanyID>").Input()));

        [Fact]
        public void Holds_For_A_Cii_Seller_With_A_Global_Identifier() =>
            Assert.Empty(CalculationRules.BrCo26(RuleDocuments.Cii().Without("SellerTradeParty/SpecifiedTaxRegistration").With("SellerTradeParty", "<ram:GlobalID schemeID=\"0088\">4000001000005</ram:GlobalID>").Input()));

        [Fact]
        public void Breaks_For_A_Seller_Without_Identifiers() =>
            Assert.Equal(["$.seller"], CalculationRules.BrCo26(RuleDocuments.Cii().Without("SellerTradeParty/SpecifiedTaxRegistration").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Seller_Identified_By_A_Sepa_Creditor_Identifier_Alone() =>
            Assert.Equal(["$.seller"], CalculationRules.BrCo26(RuleDocuments.Ubl().Without("PartyTaxScheme").With("AccountingSupplierParty/Party", "<cac:PartyIdentification><cbc:ID schemeID=\"SEPA\">DE98ZZZ09999999999</cbc:ID></cac:PartyIdentification>").Input()));
    }
}
