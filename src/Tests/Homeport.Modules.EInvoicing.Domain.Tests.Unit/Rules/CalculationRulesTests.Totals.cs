// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CalculationRulesTests
{
    private const string UblTotals = "LegalMonetaryTotal";
    private const string CiiTotals = "SpecifiedTradeSettlementHeaderMonetarySummation";

    public sealed class BrCo10
    {
        [Fact]
        public void Holds_When_The_Sum_Matches_The_Lines() => Assert.Empty(CalculationRules.BrCo10(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_When_The_Ubl_Sum_Differs_From_The_Lines() =>
            Assert.Equal(["$.documentTotals.sumOfLineNetAmounts"], CalculationRules.BrCo10(RuleDocuments.Ubl().Set($"{UblTotals}/LineExtensionAmount", "1").Input()));

        [Fact]
        public void Breaks_When_The_Cii_Sum_Differs_From_The_Lines() =>
            Assert.Equal(["$.documentTotals.sumOfLineNetAmounts"], CalculationRules.BrCo10(RuleDocuments.Cii().Set($"{CiiTotals}/LineTotalAmount", "1").Input()));

        [Fact]
        public void Breaks_Without_A_Sum() =>
            Assert.Equal(["$.documentTotals.sumOfLineNetAmounts"], CalculationRules.BrCo10(RuleDocuments.Ubl().Without($"{UblTotals}/LineExtensionAmount").Input()));

        [Fact]
        public void Holds_When_The_Lines_Round_Half_Up_To_The_Sum() =>
            Assert.Empty(CalculationRules.BrCo10(RuleDocuments.Ubl().Set("InvoiceLine/LineExtensionAmount", "0.005").Set($"{UblTotals}/LineExtensionAmount", "0.01").Input()));

        [Fact]
        public void Holds_When_Negative_Lines_Round_Half_Towards_Positive_Infinity() =>
            Assert.Empty(CalculationRules.BrCo10(RuleDocuments.Cii().Set("SpecifiedTradeSettlementLineMonetarySummation/LineTotalAmount", "-0.005").Set($"{CiiTotals}/LineTotalAmount", "0").Input()));
    }

    public sealed class BrCo11
    {
        [Fact]
        public void Holds_Without_Allowances_And_Without_A_Sum() => Assert.Empty(CalculationRules.BrCo11(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Holds_When_The_Ubl_Sum_Matches_The_Allowances() =>
            Assert.Empty(CalculationRules.BrCo11(RuleDocuments.Ubl().With("Invoice", UblAllowance).With(UblTotals, "<cbc:AllowanceTotalAmount currencyID=\"EUR\">1</cbc:AllowanceTotalAmount>").Input()));

        [Fact]
        public void Holds_When_The_Cii_Sum_Matches_The_Allowances() =>
            Assert.Empty(CalculationRules.BrCo11(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).With(CiiTotals, "<ram:AllowanceTotalAmount>1.0</ram:AllowanceTotalAmount>").Input()));

        [Fact]
        public void Holds_For_A_Sum_Of_Zero_Without_Allowances() =>
            Assert.Empty(CalculationRules.BrCo11(RuleDocuments.Ubl().With(UblTotals, "<cbc:AllowanceTotalAmount currencyID=\"EUR\">0</cbc:AllowanceTotalAmount>").Input()));

        [Fact]
        public void Breaks_For_An_Allowance_Without_A_Sum() =>
            Assert.Equal(["$.documentTotals.sumOfAllowances"], CalculationRules.BrCo11(RuleDocuments.Ubl().With("Invoice", UblAllowance).Input()));

        [Fact]
        public void Breaks_When_The_Cii_Sum_Differs_From_The_Allowances() =>
            Assert.Equal(["$.documentTotals.sumOfAllowances"], CalculationRules.BrCo11(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).With(CiiTotals, "<ram:AllowanceTotalAmount>2</ram:AllowanceTotalAmount>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Allowance_Indicated_By_0_Without_A_Sum() =>
            Assert.Equal(["$.documentTotals.sumOfAllowances"], CalculationRules.BrCo11(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).Set("SpecifiedTradeAllowanceCharge/ChargeIndicator/Indicator", "0").Input()));
    }

    public sealed class BrCo12
    {
        [Fact]
        public void Holds_When_The_Sum_Matches_The_Charges() =>
            Assert.Empty(CalculationRules.BrCo12(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCharge).With(CiiTotals, "<ram:ChargeTotalAmount>1</ram:ChargeTotalAmount>").Input()));

        [Fact]
        public void Breaks_For_A_Charge_Without_A_Sum() =>
            Assert.Equal(["$.documentTotals.sumOfCharges"], CalculationRules.BrCo12(RuleDocuments.Ubl().With("Invoice", UblCharge).Input()));

        [Fact]
        public void Breaks_For_A_Sum_Without_Charges() =>
            Assert.Equal(["$.documentTotals.sumOfCharges"], CalculationRules.BrCo12(RuleDocuments.Ubl().With(UblTotals, "<cbc:ChargeTotalAmount currencyID=\"EUR\">1</cbc:ChargeTotalAmount>").Input()));
    }

    public sealed class BrCo13
    {
        [Fact]
        public void Holds_When_The_Total_Matches_Without_Allowances_And_Charges() => Assert.Empty(CalculationRules.BrCo13(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Holds_When_The_Ubl_Total_Adds_Lines_And_Charges_Less_Allowances() =>
            Assert.Empty(CalculationRules.BrCo13(RuleDocuments.Ubl()
                .Set($"{UblTotals}/LineExtensionAmount", "10")
                .With(UblTotals, "<cbc:AllowanceTotalAmount currencyID=\"EUR\">1</cbc:AllowanceTotalAmount><cbc:ChargeTotalAmount currencyID=\"EUR\">2</cbc:ChargeTotalAmount>")
                .Set($"{UblTotals}/TaxExclusiveAmount", "11")
                .Input()));

        [Fact]
        public void Holds_When_The_Cii_Total_Subtracts_The_Allowances() =>
            Assert.Empty(CalculationRules.BrCo13(RuleDocuments.Cii()
                .Set($"{CiiTotals}/LineTotalAmount", "10")
                .With(CiiTotals, "<ram:AllowanceTotalAmount>1</ram:AllowanceTotalAmount>")
                .Set($"{CiiTotals}/TaxBasisTotalAmount", "9")
                .Input()));

        [Fact]
        public void Breaks_When_The_Total_Differs() =>
            Assert.Equal(["$.documentTotals.totalWithoutVat"], CalculationRules.BrCo13(RuleDocuments.Ubl().Set($"{UblTotals}/TaxExclusiveAmount", "1").Input()));

        [Fact]
        public void Holds_When_The_Ubl_Total_Equals_Unrounded_Lines() =>
            Assert.Empty(CalculationRules.BrCo13(RuleDocuments.Ubl().Set($"{UblTotals}/LineExtensionAmount", "0.005").Set($"{UblTotals}/TaxExclusiveAmount", "0.005").Input()));

        [Fact]
        public void Breaks_When_The_Cii_Total_Equals_Unrounded_Lines() =>
            Assert.Equal(["$.documentTotals.totalWithoutVat"], CalculationRules.BrCo13(RuleDocuments.Cii().Set($"{CiiTotals}/LineTotalAmount", "0.005").Set($"{CiiTotals}/TaxBasisTotalAmount", "0.005").Input()));
    }

    public sealed class BrCo14
    {
        [Fact]
        public void Holds_When_The_Total_Matches_The_Breakdowns() => Assert.Empty(CalculationRules.BrCo14(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_When_The_Ubl_Total_Differs_From_The_Breakdowns() =>
            Assert.Equal(["$.documentTotals.totalVatAmount"], CalculationRules.BrCo14(RuleDocuments.Ubl().Set("TaxTotal/TaxAmount", "1").Input()));

        [Fact]
        public void Breaks_When_The_Cii_Total_Differs_From_The_Breakdowns() =>
            Assert.Equal(["$.documentTotals.totalVatAmount"], CalculationRules.BrCo14(RuleDocuments.Cii().Set($"{CiiTotals}/TaxTotalAmount", "1").Input()));

        [Fact]
        public void Holds_For_A_Ubl_Tax_Total_Without_Subtotals() =>
            Assert.Empty(CalculationRules.BrCo14(RuleDocuments.Ubl().With("Invoice", "<cac:TaxTotal><cbc:TaxAmount currencyID=\"USD\">5</cbc:TaxAmount></cac:TaxTotal>").Input()));

        [Fact]
        public void Holds_For_A_Cii_Total_In_Another_Currency() =>
            Assert.Empty(CalculationRules.BrCo14(RuleDocuments.Cii().With(CiiTotals, "<ram:TaxTotalAmount currencyID=\"USD\">5</ram:TaxTotalAmount>").Input()));
    }

    public sealed class BrCo15
    {
        [Fact]
        public void Holds_When_The_Total_Adds_Up() => Assert.Empty(CalculationRules.BrCo15(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_When_The_Ubl_Total_Differs() =>
            Assert.Equal(["$.documentTotals.totalWithVat"], CalculationRules.BrCo15(RuleDocuments.Ubl().Set($"{UblTotals}/TaxInclusiveAmount", "1").Input()));

        [Fact]
        public void Breaks_When_The_Cii_Total_Differs() =>
            Assert.Equal(["$.documentTotals.totalWithVat"], CalculationRules.BrCo15(RuleDocuments.Cii().Set($"{CiiTotals}/GrandTotalAmount", "1").Input()));

        [Fact]
        public void Breaks_For_Ubl_Without_A_Total_Vat_Amount_In_The_Invoice_Currency() =>
            Assert.Equal(["$.documentTotals.totalWithVat"], CalculationRules.BrCo15(RuleDocuments.Ubl().Without("TaxTotal").Input()));

        [Fact]
        public void Holds_For_Cii_Without_A_Total_Vat_Amount_When_The_Totals_With_And_Without_Vat_Are_Equal() =>
            Assert.Empty(CalculationRules.BrCo15(RuleDocuments.Cii().Without($"{CiiTotals}/TaxTotalAmount").Input()));
    }

    public sealed class BrCo16
    {
        [Fact]
        public void Holds_When_The_Amount_Due_Adds_Up() => Assert.Empty(CalculationRules.BrCo16(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_When_The_Amount_Due_Differs() =>
            Assert.Equal(["$.documentTotals.amountDueForPayment"], CalculationRules.BrCo16(RuleDocuments.Ubl().Set($"{UblTotals}/PayableAmount", "1").Input()));

        [Fact]
        public void Holds_When_The_Ubl_Amount_Due_Subtracts_The_Paid_And_Adds_The_Rounding_Amount() =>
            Assert.Empty(CalculationRules.BrCo16(RuleDocuments.Ubl()
                .Set($"{UblTotals}/TaxInclusiveAmount", "10")
                .With(UblTotals, "<cbc:PrepaidAmount currencyID=\"EUR\">2</cbc:PrepaidAmount><cbc:PayableRoundingAmount currencyID=\"EUR\">0.01</cbc:PayableRoundingAmount>")
                .Set($"{UblTotals}/PayableAmount", "8.01")
                .Input()));

        [Fact]
        public void Holds_When_The_Ubl_Amount_Due_Is_Rounded() =>
            Assert.Empty(CalculationRules.BrCo16(RuleDocuments.Ubl()
                .Set($"{UblTotals}/TaxInclusiveAmount", "10.005")
                .With(UblTotals, "<cbc:PrepaidAmount currencyID=\"EUR\">0</cbc:PrepaidAmount>")
                .Set($"{UblTotals}/PayableAmount", "10.01")
                .Input()));

        [Fact]
        public void Breaks_When_The_Cii_Amount_Due_Is_Rounded() =>
            Assert.Equal(["$.documentTotals.amountDueForPayment"], CalculationRules.BrCo16(RuleDocuments.Cii()
                .Set($"{CiiTotals}/GrandTotalAmount", "10.005")
                .With(CiiTotals, "<ram:TotalPrepaidAmount>0</ram:TotalPrepaidAmount>")
                .Set($"{CiiTotals}/DuePayableAmount", "10.01")
                .Input()));
    }
}
