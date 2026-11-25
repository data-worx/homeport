// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CoreRulesTests
{
    private const string UblCreditTransfer = "<cac:PaymentMeans><cbc:PaymentMeansCode>58</cbc:PaymentMeansCode><cac:PayeeFinancialAccount><cbc:ID>DE02120300000000202051</cbc:ID></cac:PayeeFinancialAccount></cac:PaymentMeans>";
    private const string CiiCreditTransfer = "<ram:SpecifiedTradeSettlementPaymentMeans><ram:TypeCode>58</ram:TypeCode><ram:PayeePartyCreditorFinancialAccount><ram:IBANID>DE02120300000000202051</ram:IBANID></ram:PayeePartyCreditorFinancialAccount></ram:SpecifiedTradeSettlementPaymentMeans>";

    public sealed class Br49
    {
        [Fact]
        public void Holds_For_Payment_Means_With_A_Code() => Assert.Empty(CoreRules.Br49(RuleDocuments.Ubl().With("Invoice", UblCreditTransfer).Input()));

        [Fact]
        public void Breaks_For_A_Second_Ubl_Payment_Means_Without_A_Code() =>
            Assert.Equal(["$.paymentInstructions.paymentMeansTypeCode"], CoreRules.Br49(RuleDocuments.Ubl().With("Invoice", UblCreditTransfer + "<cac:PaymentMeans><cbc:PaymentID>R-1</cbc:PaymentID></cac:PaymentMeans>").Input()));

        [Fact]
        public void Breaks_For_Cii_Payment_Means_Without_A_Code() =>
            Assert.Equal(["$.paymentInstructions.paymentMeansTypeCode"], CoreRules.Br49(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCreditTransfer).Without("SpecifiedTradeSettlementPaymentMeans/TypeCode").Input()));
    }

    public sealed class Br50
    {
        [Fact]
        public void Holds_For_A_Credit_Transfer_With_An_Account_Identifier() =>
            Assert.Empty(CoreRules.Br50(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCreditTransfer).Input()));

        [Fact]
        public void Holds_For_A_Ubl_Code_Written_With_Spaces() =>
            Assert.Empty(CoreRules.Br50(RuleDocuments.Ubl().With("Invoice", UblCreditTransfer).Set("PaymentMeans/PaymentMeansCode", " 58 ").Set("PayeeFinancialAccount/ID", " ").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Account_With_A_Blank_Identifier() =>
            Assert.Equal(["$.paymentInstructions.creditTransfers[0].paymentAccountIdentifier"], CoreRules.Br50(RuleDocuments.Ubl().With("Invoice", UblCreditTransfer).Set("PayeeFinancialAccount/ID", " ").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Account_With_Only_A_Blank_Iban() =>
            Assert.Equal(["$.paymentInstructions.creditTransfers[0].paymentAccountIdentifier"], CoreRules.Br50(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCreditTransfer).Set("PayeePartyCreditorFinancialAccount/IBANID", " ").Input()));
    }

    public sealed class Br51
    {
        [Fact]
        public void Holds_For_The_Last_Digits_Of_A_Card_Number() =>
            Assert.Empty(CoreRules.Br51(RuleDocuments.Ubl().With("Invoice", "<cac:PaymentMeans><cbc:PaymentMeansCode>48</cbc:PaymentMeansCode><cac:CardAccount><cbc:PrimaryAccountNumberID>1234</cbc:PrimaryAccountNumberID><cbc:NetworkID>VISA</cbc:NetworkID></cac:CardAccount></cac:PaymentMeans>").Input()));

        [Fact]
        public void Breaks_For_A_Full_Card_Number() =>
            Assert.Equal(["$.paymentInstructions.paymentCard.primaryAccountNumber"], CoreRules.Br51(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:SpecifiedTradeSettlementPaymentMeans><ram:TypeCode>48</ram:TypeCode><ram:ApplicableTradeSettlementFinancialCard><ram:ID>4111111111111111</ram:ID></ram:ApplicableTradeSettlementFinancialCard></ram:SpecifiedTradeSettlementPaymentMeans>").Input()));
    }

    public sealed class Br61
    {
        [Fact]
        public void Holds_For_A_Credit_Transfer_With_An_Account() =>
            Assert.Empty(CoreRules.Br61(RuleDocuments.Ubl().With("Invoice", UblCreditTransfer).Input()));

        [Fact]
        public void Holds_For_A_Cii_Credit_Transfer_Without_An_Account() =>
            Assert.Empty(CoreRules.Br61(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCreditTransfer).Without("PayeePartyCreditorFinancialAccount").Input()));

        [Fact]
        public void Breaks_For_A_Second_Ubl_Credit_Transfer_Without_An_Account() =>
            Assert.Equal(["$.paymentInstructions.creditTransfers"], CoreRules.Br61(RuleDocuments.Ubl().With("Invoice", UblCreditTransfer + "<cac:PaymentMeans><cbc:PaymentMeansCode>30</cbc:PaymentMeansCode></cac:PaymentMeans>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Account_Without_An_Identifier() =>
            Assert.Equal(["$.paymentInstructions.creditTransfers[0].paymentAccountIdentifier"], CoreRules.Br61(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", CiiCreditTransfer).Without("PayeePartyCreditorFinancialAccount/IBANID").With("PayeePartyCreditorFinancialAccount", "<ram:AccountName>Seller</ram:AccountName>").Input()));
    }
}
