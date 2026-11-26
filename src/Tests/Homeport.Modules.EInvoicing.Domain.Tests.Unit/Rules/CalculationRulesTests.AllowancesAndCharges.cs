// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CalculationRulesTests
{
    private const string UblAllowance = "<cac:AllowanceCharge><cbc:ChargeIndicator>false</cbc:ChargeIndicator><cbc:AllowanceChargeReasonCode>95</cbc:AllowanceChargeReasonCode><cbc:AllowanceChargeReason>Discount</cbc:AllowanceChargeReason><cbc:Amount currencyID=\"EUR\">1</cbc:Amount><cac:TaxCategory><cbc:ID>S</cbc:ID><cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme></cac:TaxCategory></cac:AllowanceCharge>";
    private const string UblCharge = "<cac:AllowanceCharge><cbc:ChargeIndicator>true</cbc:ChargeIndicator><cbc:AllowanceChargeReasonCode>FC</cbc:AllowanceChargeReasonCode><cbc:Amount currencyID=\"EUR\">1</cbc:Amount><cac:TaxCategory><cbc:ID>S</cbc:ID><cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme></cac:TaxCategory></cac:AllowanceCharge>";
    private const string CiiAllowance = "<ram:SpecifiedTradeAllowanceCharge><ram:ChargeIndicator><udt:Indicator>false</udt:Indicator></ram:ChargeIndicator><ram:ActualAmount>1</ram:ActualAmount><ram:Reason>Discount</ram:Reason><ram:CategoryTradeTax><ram:TypeCode>VAT</ram:TypeCode><ram:CategoryCode>S</ram:CategoryCode></ram:CategoryTradeTax></ram:SpecifiedTradeAllowanceCharge>";
    private const string CiiCharge = "<ram:SpecifiedTradeAllowanceCharge><ram:ChargeIndicator><udt:Indicator>true</udt:Indicator></ram:ChargeIndicator><ram:ActualAmount>1</ram:ActualAmount><ram:ReasonCode>FC</ram:ReasonCode><ram:CategoryTradeTax><ram:TypeCode>VAT</ram:TypeCode><ram:CategoryCode>S</ram:CategoryCode></ram:CategoryTradeTax></ram:SpecifiedTradeAllowanceCharge>";

    // An allowance whose reason code, 95 "Discount", and reason disagree.
    private const string UblAllowanceWithDisagreeingReasons = "<cac:AllowanceCharge><cbc:ChargeIndicator>false</cbc:ChargeIndicator><cbc:AllowanceChargeReasonCode>95</cbc:AllowanceChargeReasonCode><cbc:AllowanceChargeReason>Freight</cbc:AllowanceChargeReason><cbc:Amount currencyID=\"EUR\">1</cbc:Amount></cac:AllowanceCharge>";
    private const string UblChargeWithDisagreeingReasons = "<cac:AllowanceCharge><cbc:ChargeIndicator>true</cbc:ChargeIndicator><cbc:AllowanceChargeReasonCode>FC</cbc:AllowanceChargeReasonCode><cbc:AllowanceChargeReason>Discount</cbc:AllowanceChargeReason><cbc:Amount currencyID=\"EUR\">1</cbc:Amount></cac:AllowanceCharge>";

    public sealed class BrCo05
    {
        [Fact]
        public void Holds_Even_When_Reason_Code_And_Reason_Disagree() =>
            Assert.Empty(CalculationRules.BrCo05(RuleDocuments.Ubl().With("Invoice", UblAllowanceWithDisagreeingReasons).Input()));
    }

    public sealed class BrCo06
    {
        [Fact]
        public void Holds_Even_When_Reason_Code_And_Reason_Disagree() =>
            Assert.Empty(CalculationRules.BrCo06(RuleDocuments.Ubl().With("Invoice", UblChargeWithDisagreeingReasons).Input()));
    }

    public sealed class BrCo07
    {
        [Fact]
        public void Holds_Even_When_Reason_Code_And_Reason_Disagree() =>
            Assert.Empty(CalculationRules.BrCo07(RuleDocuments.Ubl().With("InvoiceLine", UblAllowanceWithDisagreeingReasons).Input()));
    }

    public sealed class BrCo08
    {
        [Fact]
        public void Holds_Even_When_Reason_Code_And_Reason_Disagree() =>
            Assert.Empty(CalculationRules.BrCo08(RuleDocuments.Ubl().With("InvoiceLine", UblChargeWithDisagreeingReasons).Input()));
    }

    public sealed class BrCo21
    {
        [Fact]
        public void Holds_For_An_Allowance_With_A_Reason() => Assert.Empty(CalculationRules.BrCo21(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).Input()));

        [Fact]
        public void Holds_For_An_Allowance_With_A_Reason_Code_Alone() =>
            Assert.Empty(CalculationRules.BrCo21(RuleDocuments.Ubl().With("Invoice", UblAllowance).Without("AllowanceCharge/AllowanceChargeReason").Input()));

        [Fact]
        public void Breaks_For_An_Allowance_Without_Reason_And_Reason_Code() =>
            Assert.Equal(["$.documentLevelAllowances[0].reason"], CalculationRules.BrCo21(RuleDocuments.Ubl().With("Invoice", UblAllowance).Without("AllowanceCharge/AllowanceChargeReason").Without("AllowanceCharge/AllowanceChargeReasonCode").Input()));
    }

    public sealed class BrCo22
    {
        [Fact]
        public void Holds_For_A_Charge_With_A_Reason_Code() => Assert.Empty(CalculationRules.BrCo22(RuleDocuments.Ubl().With("Invoice", UblCharge).Input()));

        [Fact]
        public void Breaks_For_A_Charge_Without_Reason_And_Reason_Code() =>
            Assert.Equal(["$.documentLevelCharges[0].reason"], CalculationRules.BrCo22(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCharge).Without("SpecifiedTradeAllowanceCharge/ReasonCode").Input()));
    }

    public sealed class BrCo23
    {
        [Fact]
        public void Holds_For_A_Line_Allowance_With_A_Reason() => Assert.Empty(CalculationRules.BrCo23(RuleDocuments.Ubl().With("InvoiceLine", UblAllowance).Input()));

        [Fact]
        public void Breaks_For_A_Line_Allowance_Without_Reason_And_Reason_Code() =>
            Assert.Equal(["$.lines[0].allowances[0].reason"], CalculationRules.BrCo23(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiAllowance).Without("SpecifiedTradeAllowanceCharge/Reason").Input()));
    }

    public sealed class BrCo24
    {
        [Fact]
        public void Holds_For_A_Line_Charge_With_A_Reason_Code() => Assert.Empty(CalculationRules.BrCo24(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiCharge).Input()));

        [Fact]
        public void Breaks_For_A_Line_Charge_Without_Reason_And_Reason_Code() =>
            Assert.Equal(["$.lines[0].charges[0].reason"], CalculationRules.BrCo24(RuleDocuments.Ubl().With("InvoiceLine", UblCharge).Without("AllowanceCharge/AllowanceChargeReasonCode").Input()));
    }
}
