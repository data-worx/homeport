// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CoreRulesTests
{
    public sealed class Br01
    {
        [Fact]
        public void Holds_With_A_Specification_Identifier() => Assert.Empty(CoreRules.Br01(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_Without_A_Specification_Identifier() =>
            Assert.Equal(["$.processControl.specificationIdentifier"], CoreRules.Br01(RuleDocuments.Cii().Set("GuidelineSpecifiedDocumentContextParameter/ID", " ").Input()));
    }

    public sealed class Br02
    {
        [Fact]
        public void Holds_With_An_Invoice_Number() => Assert.Empty(CoreRules.Br02(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_An_Invoice_Number() =>
            Assert.Equal(["$.number"], CoreRules.Br02(RuleDocuments.Ubl().Without("Invoice/ID").Input()));
    }

    public sealed class Br03
    {
        [Fact]
        public void Holds_With_An_Issue_Date_In_Format_102() => Assert.Empty(CoreRules.Br03(RuleDocuments.Cii().Input()));

        [Fact]
        public void Holds_With_A_Ubl_Issue_Date_The_Model_Cannot_Read() =>
            Assert.Empty(CoreRules.Br03(RuleDocuments.Ubl().Set("IssueDate", "2026-11-25+01:00").Input()));

        [Fact]
        public void Breaks_With_A_Cii_Issue_Date_In_Another_Format() =>
            Assert.Equal(["$.issueDate"], CoreRules.Br03(RuleDocuments.Cii().With("IssueDateTime", "<udt:DateTimeString format=\"203\">202611251200</udt:DateTimeString>").Without("IssueDateTime/DateTimeString").Input()));

        [Fact]
        public void Breaks_With_A_Blank_Ubl_Issue_Date() =>
            Assert.Equal(["$.issueDate"], CoreRules.Br03(RuleDocuments.Ubl().Set("IssueDate", " ").Input()));
    }

    public sealed class Br04
    {
        [Fact]
        public void Holds_With_A_Type_Code() => Assert.Empty(CoreRules.Br04(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_Without_A_Type_Code() =>
            Assert.Equal(["$.typeCode"], CoreRules.Br04(RuleDocuments.Cii().Without("ExchangedDocument/TypeCode").Input()));
    }

    public sealed class Br05
    {
        [Fact]
        public void Holds_With_A_Currency_Code() => Assert.Empty(CoreRules.Br05(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_A_Currency_Code() =>
            Assert.Equal(["$.currencyCode"], CoreRules.Br05(RuleDocuments.Ubl().Without("DocumentCurrencyCode").Input()));
    }

    public sealed class Br12
    {
        [Fact]
        public void Holds_With_The_Sum_Of_Line_Net_Amounts() => Assert.Empty(CoreRules.Br12(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_Without_The_Sum_Of_Line_Net_Amounts() =>
            Assert.Equal(["$.documentTotals.sumOfLineNetAmounts"], CoreRules.Br12(RuleDocuments.Cii().Without("SpecifiedTradeSettlementHeaderMonetarySummation/LineTotalAmount").Input()));
    }

    public sealed class Br13
    {
        [Fact]
        public void Holds_With_The_Total_Without_Vat() => Assert.Empty(CoreRules.Br13(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_The_Total_Without_Vat() =>
            Assert.Equal(["$.documentTotals.totalWithoutVat"], CoreRules.Br13(RuleDocuments.Ubl().Without("LegalMonetaryTotal/TaxExclusiveAmount").Input()));
    }

    public sealed class Br14
    {
        [Fact]
        public void Holds_With_The_Total_With_Vat() => Assert.Empty(CoreRules.Br14(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_Without_The_Total_With_Vat() =>
            Assert.Equal(["$.documentTotals.totalWithVat"], CoreRules.Br14(RuleDocuments.Cii().Without("SpecifiedTradeSettlementHeaderMonetarySummation/GrandTotalAmount").Input()));
    }

    public sealed class Br15
    {
        [Fact]
        public void Holds_With_The_Amount_Due() => Assert.Empty(CoreRules.Br15(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_The_Amount_Due() =>
            Assert.Equal(["$.documentTotals.amountDueForPayment"], CoreRules.Br15(RuleDocuments.Ubl().Without("LegalMonetaryTotal/PayableAmount").Input()));
    }

    public sealed class Br16
    {
        [Fact]
        public void Holds_With_A_Line() => Assert.Empty(CoreRules.Br16(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_Without_A_Line() =>
            Assert.Equal(["$.lines"], CoreRules.Br16(RuleDocuments.Cii().Without("IncludedSupplyChainTradeLineItem").Input()));
    }

    public sealed class Br29
    {
        [Fact]
        public void Holds_When_The_Ubl_Period_Ends_On_Its_Start() =>
            Assert.Empty(CoreRules.Br29(RuleDocuments.Ubl().With("Invoice", "<cac:InvoicePeriod><cbc:StartDate>2026-11-01</cbc:StartDate><cbc:EndDate>2026-11-01</cbc:EndDate></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Holds_When_The_Period_Has_No_End() =>
            Assert.Empty(CoreRules.Br29(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:BillingSpecifiedPeriod><ram:StartDateTime><udt:DateTimeString format=\"102\">20261130</udt:DateTimeString></ram:StartDateTime></ram:BillingSpecifiedPeriod>").Input()));

        [Fact]
        public void Breaks_When_The_Ubl_Period_Ends_Before_Its_Start() =>
            Assert.Equal(["$.invoicingPeriod"], CoreRules.Br29(RuleDocuments.Ubl().With("Invoice", "<cac:InvoicePeriod><cbc:StartDate>2026-11-30</cbc:StartDate><cbc:EndDate>2026-11-01</cbc:EndDate></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Holds_When_A_Ubl_Line_Period_Ends_Before_Its_Start_Which_BR_30_Checks() =>
            Assert.Empty(CoreRules.Br29(RuleDocuments.Ubl().With("InvoiceLine", "<cac:InvoicePeriod><cbc:StartDate>2026-11-30</cbc:StartDate><cbc:EndDate>2026-11-01</cbc:EndDate></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Breaks_When_The_Cii_Period_Ends_Before_Its_Start() =>
            Assert.Equal(["$.invoicingPeriod"], CoreRules.Br29(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:BillingSpecifiedPeriod><ram:StartDateTime><udt:DateTimeString format=\"102\">20261130</udt:DateTimeString></ram:StartDateTime><ram:EndDateTime><udt:DateTimeString format=\"102\">20261101</udt:DateTimeString></ram:EndDateTime></ram:BillingSpecifiedPeriod>").Input()));

        [Fact]
        public void Breaks_When_A_Cii_Date_Is_In_Another_Format() =>
            Assert.Equal(["$.invoicingPeriod"], CoreRules.Br29(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:BillingSpecifiedPeriod><ram:StartDateTime><udt:DateTimeString format=\"102\">20261101</udt:DateTimeString></ram:StartDateTime><ram:EndDateTime><udt:DateTimeString format=\"610\">202611</udt:DateTimeString></ram:EndDateTime></ram:BillingSpecifiedPeriod>").Input()));
    }

    public sealed class Br53
    {
        [Fact]
        public void Holds_When_Ubl_Carries_A_Tax_Total_In_The_Accounting_Currency() =>
            Assert.Empty(CoreRules.Br53(RuleDocuments.Ubl().With("Invoice", "<cbc:TaxCurrencyCode>EUR</cbc:TaxCurrencyCode><cac:TaxTotal><cbc:TaxAmount currencyID=\"EUR\">0</cbc:TaxAmount></cac:TaxTotal>").Input()));

        [Fact]
        public void Breaks_When_Ubl_Carries_No_Tax_Total_In_The_Accounting_Currency() =>
            Assert.Equal(["$.documentTotals.totalVatAmountInAccountingCurrency"], CoreRules.Br53(RuleDocuments.Ubl().With("Invoice", "<cbc:TaxCurrencyCode>SEK</cbc:TaxCurrencyCode><cac:TaxTotal><cbc:TaxAmount currencyID=\"EUR\">0</cbc:TaxAmount></cac:TaxTotal>").Input()));

        [Fact]
        public void Holds_When_Cii_Carries_The_Tax_Total_In_Another_Accounting_Currency() =>
            Assert.Empty(CoreRules.Br53(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:TaxCurrencyCode>SEK</ram:TaxCurrencyCode>").With("SpecifiedTradeSettlementHeaderMonetarySummation", "<ram:TaxTotalAmount currencyID=\"SEK\">0</ram:TaxTotalAmount>").Input()));

        [Fact]
        public void Breaks_When_The_Cii_Accounting_Currency_Is_The_Invoice_Currency() =>
            Assert.Equal(["$.documentTotals.totalVatAmountInAccountingCurrency"], CoreRules.Br53(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:TaxCurrencyCode>EUR</ram:TaxCurrencyCode>").With("SpecifiedTradeSettlementHeaderMonetarySummation", "<ram:TaxTotalAmount currencyID=\"EUR\">0</ram:TaxTotalAmount>").Input()));
    }
}
