// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed class DecimalRulesTests
{
    private const string UblTotals = "LegalMonetaryTotal";
    private const string CiiTotals = "SpecifiedTradeSettlementHeaderMonetarySummation";
    private const string UblAllowance = "<cac:AllowanceCharge><cbc:ChargeIndicator>false</cbc:ChargeIndicator><cbc:AllowanceChargeReason>Discount</cbc:AllowanceChargeReason><cbc:Amount currencyID=\"EUR\">1.00</cbc:Amount><cbc:BaseAmount currencyID=\"EUR\">10.00</cbc:BaseAmount></cac:AllowanceCharge>";
    private const string UblCharge = "<cac:AllowanceCharge><cbc:ChargeIndicator>true</cbc:ChargeIndicator><cbc:AllowanceChargeReasonCode>FC</cbc:AllowanceChargeReasonCode><cbc:Amount currencyID=\"EUR\">1.00</cbc:Amount><cbc:BaseAmount currencyID=\"EUR\">10.00</cbc:BaseAmount></cac:AllowanceCharge>";
    private const string CiiAllowance = "<ram:SpecifiedTradeAllowanceCharge><ram:ChargeIndicator><udt:Indicator>false</udt:Indicator></ram:ChargeIndicator><ram:BasisAmount>10.00</ram:BasisAmount><ram:ActualAmount>1.00</ram:ActualAmount><ram:Reason>Discount</ram:Reason></ram:SpecifiedTradeAllowanceCharge>";
    private const string CiiCharge = "<ram:SpecifiedTradeAllowanceCharge><ram:ChargeIndicator><udt:Indicator>true</udt:Indicator></ram:ChargeIndicator><ram:BasisAmount>10.00</ram:BasisAmount><ram:ActualAmount>1.00</ram:ActualAmount><ram:ReasonCode>FC</ram:ReasonCode></ram:SpecifiedTradeAllowanceCharge>";
    private const string TooPrecise = "1.005";

    public sealed class BrDec01
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec01(RuleDocuments.Ubl().With("Invoice", UblAllowance).Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentLevelAllowances[0].amount"], DecimalRules.BrDec01(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).Set("SpecifiedTradeAllowanceCharge/ActualAmount", TooPrecise).Input()));
    }

    public sealed class BrDec02
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec02(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentLevelAllowances[0].baseAmount"], DecimalRules.BrDec02(RuleDocuments.Ubl().With("Invoice", UblAllowance).Set("AllowanceCharge/BaseAmount", TooPrecise).Input()));
    }

    public sealed class BrDec05
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec05(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCharge).Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentLevelCharges[0].amount"], DecimalRules.BrDec05(RuleDocuments.Ubl().With("Invoice", UblCharge).Set("AllowanceCharge/Amount", TooPrecise).Input()));
    }

    public sealed class BrDec06
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec06(RuleDocuments.Ubl().With("Invoice", UblCharge).Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentLevelCharges[0].baseAmount"], DecimalRules.BrDec06(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCharge).Set("SpecifiedTradeAllowanceCharge/BasisAmount", TooPrecise).Input()));
    }

    public sealed class BrDec09
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec09(RuleDocuments.Ubl().Set($"{UblTotals}/LineExtensionAmount", "1.00").Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.sumOfLineNetAmounts"], DecimalRules.BrDec09(RuleDocuments.Cii().Set($"{CiiTotals}/LineTotalAmount", TooPrecise).Input()));

        [Fact]
        public void Breaks_For_Trailing_Zeros_Beyond_Two_Decimals() =>
            Assert.Equal(["$.documentTotals.sumOfLineNetAmounts"], DecimalRules.BrDec09(RuleDocuments.Ubl().Set($"{UblTotals}/LineExtensionAmount", "1.000").Input()));

        [Fact]
        public void Breaks_For_Two_Decimals_Followed_By_A_Space() =>
            Assert.Equal(["$.documentTotals.sumOfLineNetAmounts"], DecimalRules.BrDec09(RuleDocuments.Ubl().Set($"{UblTotals}/LineExtensionAmount", "1.00 ").Input()));
    }

    public sealed class BrDec10
    {
        [Fact]
        public void Holds_Without_The_Amount() => Assert.Empty(DecimalRules.BrDec10(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.sumOfAllowances"], DecimalRules.BrDec10(RuleDocuments.Ubl().With(UblTotals, $"<cbc:AllowanceTotalAmount currencyID=\"EUR\">{TooPrecise}</cbc:AllowanceTotalAmount>").Input()));
    }

    public sealed class BrDec11
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec11(RuleDocuments.Cii().With(CiiTotals, "<ram:ChargeTotalAmount>1.00</ram:ChargeTotalAmount>").Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.sumOfCharges"], DecimalRules.BrDec11(RuleDocuments.Cii().With(CiiTotals, $"<ram:ChargeTotalAmount>{TooPrecise}</ram:ChargeTotalAmount>").Input()));
    }

    public sealed class BrDec12
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec12(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.totalWithoutVat"], DecimalRules.BrDec12(RuleDocuments.Ubl().Set($"{UblTotals}/TaxExclusiveAmount", TooPrecise).Input()));
    }

    public sealed class BrDec13
    {
        [Fact]
        public void Holds_For_A_Cii_Amount_With_Trailing_Zeros_Beyond_Two_Decimals() =>
            Assert.Empty(DecimalRules.BrDec13(RuleDocuments.Cii().Set($"{CiiTotals}/TaxTotalAmount", "1.000").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Amount_In_The_Invoice_Currency_With_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.totalVatAmount"], DecimalRules.BrDec13(RuleDocuments.Cii().Set($"{CiiTotals}/TaxTotalAmount", TooPrecise).Input()));

        [Fact]
        public void Holds_For_A_Cii_Amount_In_Another_Currency_Beside_It() =>
            Assert.Empty(DecimalRules.BrDec13(RuleDocuments.Cii().Set($"{CiiTotals}/TaxTotalAmount", TooPrecise).With(CiiTotals, "<ram:TaxTotalAmount currencyID=\"USD\">1.00</ram:TaxTotalAmount>").Input()));

        [Fact]
        public void Holds_For_Ubl_Whose_Test_Never_Fails() =>
            Assert.Empty(DecimalRules.BrDec13(RuleDocuments.Ubl().Set("TaxTotal/TaxAmount", TooPrecise).Input()));
    }

    public sealed class BrDec14
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec14(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.totalWithVat"], DecimalRules.BrDec14(RuleDocuments.Cii().Set($"{CiiTotals}/GrandTotalAmount", TooPrecise).Input()));
    }

    public sealed class BrDec15
    {
        private const string UsdAccounting = "<ram:TaxCurrencyCode>USD</ram:TaxCurrencyCode>";

        [Fact]
        public void Holds_For_A_Cii_Amount_In_The_Accounting_Currency_With_Two_Decimals() =>
            Assert.Empty(DecimalRules.BrDec15(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", UsdAccounting).With(CiiTotals, "<ram:TaxTotalAmount currencyID=\"USD\">1.00</ram:TaxTotalAmount>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Amount_In_The_Accounting_Currency_With_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.totalVatAmountInAccountingCurrency"], DecimalRules.BrDec15(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", UsdAccounting).With(CiiTotals, $"<ram:TaxTotalAmount currencyID=\"USD\">{TooPrecise}</ram:TaxTotalAmount>").Input()));

        [Fact]
        public void Holds_For_Cii_Without_An_Accounting_Currency() =>
            Assert.Empty(DecimalRules.BrDec15(RuleDocuments.Cii().Set($"{CiiTotals}/TaxTotalAmount", TooPrecise).Input()));

        [Fact]
        public void Holds_For_Ubl_Whose_Test_Never_Fails() =>
            Assert.Empty(DecimalRules.BrDec15(RuleDocuments.Ubl().With("Invoice", $"<cbc:TaxCurrencyCode>USD</cbc:TaxCurrencyCode><cac:TaxTotal><cbc:TaxAmount currencyID=\"USD\">{TooPrecise}</cbc:TaxAmount></cac:TaxTotal>").Input()));
    }

    public sealed class BrDec16
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec16(RuleDocuments.Ubl().With(UblTotals, "<cbc:PrepaidAmount currencyID=\"EUR\">1.00</cbc:PrepaidAmount>").Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.paidAmount"], DecimalRules.BrDec16(RuleDocuments.Cii().With(CiiTotals, $"<ram:TotalPrepaidAmount>{TooPrecise}</ram:TotalPrepaidAmount>").Input()));
    }

    public sealed class BrDec17
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec17(RuleDocuments.Cii().With(CiiTotals, "<ram:RoundingAmount>0.01</ram:RoundingAmount>").Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.roundingAmount"], DecimalRules.BrDec17(RuleDocuments.Ubl().With(UblTotals, $"<cbc:PayableRoundingAmount currencyID=\"EUR\">{TooPrecise}</cbc:PayableRoundingAmount>").Input()));
    }

    public sealed class BrDec18
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec18(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.documentTotals.amountDueForPayment"], DecimalRules.BrDec18(RuleDocuments.Ubl().Set($"{UblTotals}/PayableAmount", TooPrecise).Input()));
    }

    public sealed class BrDec19
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec19(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.vatBreakdowns[0].taxableAmount"], DecimalRules.BrDec19(RuleDocuments.Cii().Set("ApplicableHeaderTradeSettlement/ApplicableTradeTax/BasisAmount", TooPrecise).Input()));

        [Fact]
        public void Holds_For_A_Cii_Vat_Breakdown_Of_A_Category_A_Rule_Of_Its_Own_Checks() =>
            Assert.Empty(DecimalRules.BrDec19(RuleDocuments.Cii().Set("ApplicableHeaderTradeSettlement/ApplicableTradeTax/CategoryCode", "M").Set("ApplicableHeaderTradeSettlement/ApplicableTradeTax/BasisAmount", TooPrecise).Input()));
    }

    public sealed class BrDec20
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec20(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.vatBreakdowns[0].taxAmount"], DecimalRules.BrDec20(RuleDocuments.Ubl().Set("TaxSubtotal/TaxAmount", TooPrecise).Input()));
    }

    public sealed class BrDec23
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec23(RuleDocuments.Ubl().Set("InvoiceLine/LineExtensionAmount", "1.00").Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.lines[0].netAmount"], DecimalRules.BrDec23(RuleDocuments.Cii().Set("SpecifiedTradeSettlementLineMonetarySummation/LineTotalAmount", TooPrecise).Input()));
    }

    public sealed class BrDec24
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec24(RuleDocuments.Ubl().With("InvoiceLine", UblAllowance).Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.lines[0].allowances[0].amount"], DecimalRules.BrDec24(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiAllowance).Set("SpecifiedTradeAllowanceCharge/ActualAmount", TooPrecise).Input()));
    }

    public sealed class BrDec25
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec25(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiAllowance).Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.lines[0].allowances[0].baseAmount"], DecimalRules.BrDec25(RuleDocuments.Ubl().With("InvoiceLine", UblAllowance).Set("AllowanceCharge/BaseAmount", TooPrecise).Input()));
    }

    public sealed class BrDec27
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec27(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiCharge).Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.lines[0].charges[0].amount"], DecimalRules.BrDec27(RuleDocuments.Ubl().With("InvoiceLine", UblCharge).Set("AllowanceCharge/Amount", TooPrecise).Input()));
    }

    public sealed class BrDec28
    {
        [Fact]
        public void Holds_For_Two_Decimals() => Assert.Empty(DecimalRules.BrDec28(RuleDocuments.Ubl().With("InvoiceLine", UblCharge).Input()));

        [Fact]
        public void Breaks_For_Three_Decimals() =>
            Assert.Equal(["$.lines[0].charges[0].baseAmount"], DecimalRules.BrDec28(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiCharge).Set("SpecifiedTradeAllowanceCharge/BasisAmount", TooPrecise).Input()));
    }
}
