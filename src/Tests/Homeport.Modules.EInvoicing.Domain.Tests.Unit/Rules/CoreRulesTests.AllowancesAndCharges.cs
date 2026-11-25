// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CoreRulesTests
{
    private const string UblAllowance = "<cac:AllowanceCharge><cbc:ChargeIndicator>false</cbc:ChargeIndicator><cbc:AllowanceChargeReason>Discount</cbc:AllowanceChargeReason><cbc:Amount currencyID=\"EUR\">1</cbc:Amount><cac:TaxCategory><cbc:ID>S</cbc:ID><cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme></cac:TaxCategory></cac:AllowanceCharge>";
    private const string UblAllowanceWithoutAmount = "<cac:AllowanceCharge><cbc:ChargeIndicator>false</cbc:ChargeIndicator><cbc:AllowanceChargeReason>Discount</cbc:AllowanceChargeReason><cac:TaxCategory><cbc:ID>S</cbc:ID><cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme></cac:TaxCategory></cac:AllowanceCharge>";
    private const string UblCharge = "<cac:AllowanceCharge><cbc:ChargeIndicator>true</cbc:ChargeIndicator><cbc:AllowanceChargeReasonCode>FC</cbc:AllowanceChargeReasonCode><cbc:Amount currencyID=\"EUR\">1</cbc:Amount><cac:TaxCategory><cbc:ID>S</cbc:ID><cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme></cac:TaxCategory></cac:AllowanceCharge>";
    private const string CiiAllowance = "<ram:SpecifiedTradeAllowanceCharge><ram:ChargeIndicator><udt:Indicator>false</udt:Indicator></ram:ChargeIndicator><ram:ActualAmount>1</ram:ActualAmount><ram:Reason>Discount</ram:Reason><ram:CategoryTradeTax><ram:TypeCode>VAT</ram:TypeCode><ram:CategoryCode>S</ram:CategoryCode></ram:CategoryTradeTax></ram:SpecifiedTradeAllowanceCharge>";
    private const string CiiCharge = "<ram:SpecifiedTradeAllowanceCharge><ram:ChargeIndicator><udt:Indicator>true</udt:Indicator></ram:ChargeIndicator><ram:ActualAmount>1</ram:ActualAmount><ram:ReasonCode>FC</ram:ReasonCode><ram:CategoryTradeTax><ram:TypeCode>VAT</ram:TypeCode><ram:CategoryCode>S</ram:CategoryCode></ram:CategoryTradeTax></ram:SpecifiedTradeAllowanceCharge>";

    public sealed class Br31
    {
        [Fact]
        public void Holds_For_An_Allowance_With_An_Amount() => Assert.Empty(CoreRules.Br31(RuleDocuments.Ubl().With("Invoice", UblAllowance).Input()));

        [Fact]
        public void Holds_For_A_Ubl_Allowance_Indicated_By_0_With_An_Amount() =>
            Assert.Empty(CoreRules.Br31(RuleDocuments.Ubl().With("Invoice", UblAllowance).Set("AllowanceCharge/ChargeIndicator", "0").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Allowance_Without_An_Amount() =>
            Assert.Equal(["$.documentLevelAllowances[1].amount"], CoreRules.Br31(RuleDocuments.Ubl().With("Invoice", UblCharge + UblAllowance + UblAllowanceWithoutAmount).Input()));

        [Fact]
        public void Breaks_For_A_Cii_Allowance_Without_An_Amount() =>
            Assert.Equal(["$.documentLevelAllowances[0].amount"], CoreRules.Br31(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).Without("SpecifiedTradeAllowanceCharge/ActualAmount").Input()));
    }

    public sealed class Br32
    {
        [Fact]
        public void Holds_For_An_Allowance_With_A_Vat_Category() => Assert.Empty(CoreRules.Br32(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Allowance_Whose_Category_Is_Under_Another_Tax_Scheme() =>
            Assert.Equal(["$.documentLevelAllowances[0].vatCategoryCode"], CoreRules.Br32(RuleDocuments.Ubl().With("Invoice", UblAllowance).Set("AllowanceCharge/TaxCategory/TaxScheme/ID", "GST").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Allowance_Whose_Tax_Is_Of_Another_Type() =>
            Assert.Equal(["$.documentLevelAllowances[0].vatCategoryCode"], CoreRules.Br32(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).Set("CategoryTradeTax/TypeCode", "GST").Input()));
    }

    public sealed class Br33
    {
        [Fact]
        public void Holds_For_An_Allowance_With_A_Blank_Reason() =>
            Assert.Empty(CoreRules.Br33(RuleDocuments.Ubl().With("Invoice", UblAllowance).Set("AllowanceCharge/AllowanceChargeReason", " ").Input()));

        [Fact]
        public void Breaks_For_An_Allowance_Without_A_Reason() =>
            Assert.Equal(["$.documentLevelAllowances[0].reason"], CoreRules.Br33(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiAllowance).Without("SpecifiedTradeAllowanceCharge/Reason").Input()));
    }

    public sealed class Br36
    {
        [Fact]
        public void Holds_For_A_Charge_With_An_Amount() => Assert.Empty(CoreRules.Br36(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCharge).Input()));

        [Fact]
        public void Holds_For_A_Cii_Charge_Indicated_By_1_Without_An_Amount() =>
            Assert.Empty(CoreRules.Br36(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCharge).Set("ChargeIndicator/Indicator", "1").Without("SpecifiedTradeAllowanceCharge/ActualAmount").Input()));

        [Fact]
        public void Breaks_For_A_Charge_Without_An_Amount() =>
            Assert.Equal(["$.documentLevelCharges[0].amount"], CoreRules.Br36(RuleDocuments.Ubl().With("Invoice", UblCharge).Without("AllowanceCharge/Amount").Input()));
    }

    public sealed class Br37
    {
        [Fact]
        public void Holds_For_A_Charge_With_A_Vat_Category() => Assert.Empty(CoreRules.Br37(RuleDocuments.Ubl().With("Invoice", UblCharge).Input()));

        [Fact]
        public void Breaks_For_A_Charge_Without_A_Vat_Category() =>
            Assert.Equal(["$.documentLevelCharges[0].vatCategoryCode"], CoreRules.Br37(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCharge).Without("CategoryTradeTax/CategoryCode").Input()));
    }

    public sealed class Br38
    {
        [Fact]
        public void Holds_For_A_Charge_With_A_Reason_Code() => Assert.Empty(CoreRules.Br38(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCharge).Input()));

        [Fact]
        public void Breaks_For_A_Charge_Without_A_Reason() =>
            Assert.Equal(["$.documentLevelCharges[0].reason"], CoreRules.Br38(RuleDocuments.Ubl().With("Invoice", UblCharge).Without("AllowanceCharge/AllowanceChargeReasonCode").Input()));
    }

    public sealed class Br41
    {
        [Fact]
        public void Holds_For_A_Line_Allowance_With_An_Amount() => Assert.Empty(CoreRules.Br41(RuleDocuments.Ubl().With("InvoiceLine", UblAllowance).Input()));

        [Fact]
        public void Breaks_For_A_Line_Allowance_Without_An_Amount() =>
            Assert.Equal(["$.lines[0].allowances[0].amount"], CoreRules.Br41(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiAllowance).Without("SpecifiedTradeAllowanceCharge/ActualAmount").Input()));
    }

    public sealed class Br42
    {
        [Fact]
        public void Holds_For_A_Line_Allowance_With_A_Reason() => Assert.Empty(CoreRules.Br42(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiAllowance).Input()));

        [Fact]
        public void Breaks_For_A_Line_Allowance_Without_A_Reason() =>
            Assert.Equal(["$.lines[0].allowances[0].reason"], CoreRules.Br42(RuleDocuments.Ubl().With("InvoiceLine", UblAllowance).Without("AllowanceCharge/AllowanceChargeReason").Input()));
    }

    public sealed class Br43
    {
        [Fact]
        public void Holds_For_A_Line_Charge_With_An_Amount() => Assert.Empty(CoreRules.Br43(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiCharge).Input()));

        [Fact]
        public void Breaks_For_A_Line_Charge_Without_An_Amount() =>
            Assert.Equal(["$.lines[0].charges[0].amount"], CoreRules.Br43(RuleDocuments.Ubl().With("InvoiceLine", UblCharge).Without("AllowanceCharge/Amount").Input()));
    }

    public sealed class Br44
    {
        [Fact]
        public void Holds_For_A_Line_Charge_With_A_Reason_Code() => Assert.Empty(CoreRules.Br44(RuleDocuments.Ubl().With("InvoiceLine", UblCharge).Input()));

        [Fact]
        public void Breaks_For_A_Line_Charge_Without_A_Reason() =>
            Assert.Equal(["$.lines[0].charges[0].reason"], CoreRules.Br44(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", CiiCharge).Without("SpecifiedTradeAllowanceCharge/ReasonCode").Input()));
    }
}
