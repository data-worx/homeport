// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CalculationRulesTests
{
    private const string CiiHeaderTax = "ApplicableHeaderTradeSettlement/ApplicableTradeTax";
    private const string CiiTaxS = "<ram:ApplicableTradeTax><ram:CalculatedAmount>0</ram:CalculatedAmount><ram:TypeCode>VAT</ram:TypeCode><ram:BasisAmount>0</ram:BasisAmount><ram:CategoryCode>S</ram:CategoryCode><ram:RateApplicablePercent>7</ram:RateApplicablePercent></ram:ApplicableTradeTax>";
    private const string CiiVatPointDateAndCode = "<ram:TaxPointDate><udt:DateString format=\"102\">20261101</udt:DateString></ram:TaxPointDate><ram:DueDateTypeCode>5</ram:DueDateTypeCode>";

    public sealed class BrCo03
    {
        [Fact]
        public void Holds_Without_A_Vat_Point_Date_And_Code() => Assert.Empty(CalculationRules.BrCo03(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Holds_With_A_Vat_Point_Date_Alone() =>
            Assert.Empty(CalculationRules.BrCo03(RuleDocuments.Ubl().With("Invoice", "<cbc:TaxPointDate>2026-11-01</cbc:TaxPointDate>").Input()));

        [Fact]
        public void Breaks_Once_For_A_Ubl_Invoice_With_Both() =>
            Assert.Equal(["$.vatPointDate"], CalculationRules.BrCo03(RuleDocuments.Ubl().With("Invoice", "<cbc:TaxPointDate>2026-11-01</cbc:TaxPointDate><cac:InvoicePeriod><cbc:DescriptionCode>35</cbc:DescriptionCode></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Breaks_Once_For_Every_Cii_Vat_Breakdown_With_Both() =>
            Assert.Equal(["$.vatPointDate", "$.vatPointDate"], CalculationRules.BrCo03(RuleDocuments.Cii().With(CiiHeaderTax, CiiVatPointDateAndCode).With("ApplicableHeaderTradeSettlement", CiiTaxS).Input()));

        [Fact]
        public void Holds_For_A_Cii_Vat_Breakdown_Of_A_Category_A_Rule_Of_Its_Own_Checks() =>
            Assert.Empty(CalculationRules.BrCo03(RuleDocuments.Cii().With(CiiHeaderTax, CiiVatPointDateAndCode).Set($"{CiiHeaderTax}/CategoryCode", "O").Input()));
    }

    public sealed class BrCo04
    {
        [Fact]
        public void Holds_With_A_Vat_Category_Code() => Assert.Empty(CalculationRules.BrCo04(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Category_Under_Another_Tax_Scheme() =>
            Assert.Equal(["$.lines[0].vatInformation.vatCategoryCode"], CalculationRules.BrCo04(RuleDocuments.Ubl().Set("ClassifiedTaxCategory/TaxScheme/ID", "GST").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Line_Without_A_Trade_Tax() =>
            Assert.Equal(["$.lines[0].vatInformation.vatCategoryCode"], CalculationRules.BrCo04(RuleDocuments.Cii().Without("SpecifiedLineTradeSettlement/ApplicableTradeTax").Input()));
    }

    public sealed class BrCo17
    {
        [Fact]
        public void Holds_When_The_Tax_Amount_Is_The_Taxable_Amount_Times_The_Rate() =>
            Assert.Empty(CalculationRules.BrCo17(RuleDocuments.Ubl().Set("TaxSubtotal/TaxableAmount", "100").Set("TaxSubtotal/TaxAmount", "19").Input()));

        [Fact]
        public void Breaks_When_The_Tax_Amount_Is_Far_Off() =>
            Assert.Equal(["$.vatBreakdowns[0].taxAmount"], CalculationRules.BrCo17(RuleDocuments.Cii().Set($"{CiiHeaderTax}/BasisAmount", "100").Set($"{CiiHeaderTax}/CalculatedAmount", "25").Input()));

        [Fact]
        public void Breaks_When_The_Ubl_Tax_Amount_Is_One_Unit_Off() =>
            Assert.Equal(["$.vatBreakdowns[0].taxAmount"], CalculationRules.BrCo17(RuleDocuments.Ubl().Set("TaxSubtotal/TaxableAmount", "100").Set("TaxSubtotal/TaxAmount", "20").Input()));

        [Fact]
        public void Holds_When_The_Cii_Tax_Amount_Is_One_Unit_Off() =>
            Assert.Empty(CalculationRules.BrCo17(RuleDocuments.Cii().Set($"{CiiHeaderTax}/BasisAmount", "100").Set($"{CiiHeaderTax}/CalculatedAmount", "20").Input()));

        [Fact]
        public void Breaks_For_A_Rate_That_Rounds_To_Zero_With_A_Tax_Amount_That_Does_Not() =>
            Assert.Equal(["$.vatBreakdowns[0].taxAmount"], CalculationRules.BrCo17(RuleDocuments.Ubl().Set("TaxSubtotal/TaxCategory/Percent", "0.4").Set("TaxSubtotal/TaxableAmount", "200").Set("TaxSubtotal/TaxAmount", "0.8").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Tax_Of_Another_Type_With_A_Tax_Amount() =>
            Assert.Equal(["$.vatBreakdowns[0].taxAmount"], CalculationRules.BrCo17(RuleDocuments.Cii().Set($"{CiiHeaderTax}/TypeCode", "GST").Set($"{CiiHeaderTax}/BasisAmount", "100").Set($"{CiiHeaderTax}/CalculatedAmount", "19").Input()));

        [Fact]
        public void Holds_For_A_Cii_Vat_Breakdown_Of_A_Category_A_Rule_Of_Its_Own_Checks() =>
            Assert.Empty(CalculationRules.BrCo17(RuleDocuments.Cii().Set($"{CiiHeaderTax}/CategoryCode", "L").Set($"{CiiHeaderTax}/CalculatedAmount", "25").Input()));
    }

    public sealed class BrCo18
    {
        [Fact]
        public void Holds_With_A_Vat_Breakdown() => Assert.Empty(CalculationRules.BrCo18(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_For_Ubl_Without_A_Vat_Breakdown() =>
            Assert.Equal(["$.vatBreakdowns"], CalculationRules.BrCo18(RuleDocuments.Ubl().Without("TaxTotal").Input()));

        [Fact]
        public void Breaks_Once_For_Every_Cii_Line_Without_A_Vat_Breakdown() =>
            Assert.Equal(["$.vatBreakdowns"], CalculationRules.BrCo18(RuleDocuments.Cii().Without(CiiHeaderTax).Input()));

        [Fact]
        public void Holds_For_Cii_Without_A_Vat_Breakdown_And_Without_Lines() =>
            Assert.Empty(CalculationRules.BrCo18(RuleDocuments.Cii().Without(CiiHeaderTax).Without("IncludedSupplyChainTradeLineItem").Input()));
    }
}
