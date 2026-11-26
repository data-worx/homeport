// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CalculationRulesTests
{
    private const string CiiPeriodWithEnd = "<ram:BillingSpecifiedPeriod><ram:EndDateTime><udt:DateTimeString format=\"102\">20261130</udt:DateTimeString></ram:EndDateTime></ram:BillingSpecifiedPeriod>";

    public sealed class BrCo19
    {
        [Fact]
        public void Holds_For_A_Period_With_An_End_Date() =>
            Assert.Empty(CalculationRules.BrCo19(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiPeriodWithEnd).Input()));

        [Fact]
        public void Holds_For_A_Ubl_Period_With_A_Vat_Point_Date_Code_Alone() =>
            Assert.Empty(CalculationRules.BrCo19(RuleDocuments.Ubl().With("Invoice", "<cac:InvoicePeriod><cbc:DescriptionCode>35</cbc:DescriptionCode></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Breaks_For_A_Period_Without_Dates() =>
            Assert.Equal(["$.invoicingPeriod"], CalculationRules.BrCo19(RuleDocuments.Ubl().With("Invoice", "<cac:InvoicePeriod/>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Period_Without_Dates() =>
            Assert.Equal(["$.invoicingPeriod"], CalculationRules.BrCo19(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:BillingSpecifiedPeriod/>").Input()));

        [Fact]
        public void Holds_For_A_Ubl_Line_Period_Without_Dates_Which_BR_CO_20_Checks() =>
            Assert.Empty(CalculationRules.BrCo19(RuleDocuments.Ubl().With("InvoiceLine", "<cac:InvoicePeriod/>").Input()));
    }

    public sealed class BrCo20
    {
        [Fact]
        public void Holds_For_A_Line_Period_With_A_Start_Date() =>
            Assert.Empty(CalculationRules.BrCo20(RuleDocuments.Ubl().With("InvoiceLine", "<cac:InvoicePeriod><cbc:StartDate>2026-11-01</cbc:StartDate></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Line_Period_Without_Dates() =>
            Assert.Equal(["$.lines[0].period"], CalculationRules.BrCo20(RuleDocuments.Ubl().With("InvoiceLine", "<cac:InvoicePeriod/>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Line_Period_Without_Dates() =>
            Assert.Equal(["$.lines[0].period"], CalculationRules.BrCo20(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", "<ram:BillingSpecifiedPeriod/>").Input()));

        [Fact]
        public void Holds_For_An_Invoicing_Period_Without_Dates() =>
            Assert.Empty(CalculationRules.BrCo20(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:BillingSpecifiedPeriod/>").Input()));
    }
}
