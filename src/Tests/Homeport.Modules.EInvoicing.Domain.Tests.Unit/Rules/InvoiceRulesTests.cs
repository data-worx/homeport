// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.Rules;
using Homeport.Modules.EInvoicing.Domain.RuleSets;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed class InvoiceRulesTests
{
    public sealed class Validate
    {
        private static readonly RuleCatalog Catalog = RuleCatalog.LoadEmbedded();

        [Fact]
        public void Finds_Nothing_On_An_Invoice_That_Breaks_No_Rule() =>
            Assert.Empty(InvoiceRules.Validate(RuleDocuments.Cii().Input(), RuleSet.XRechnung, Catalog));

        [Fact]
        public void Reports_A_Broken_Rule_With_Its_Message_Path_And_Business_Term() =>
            Assert.Equal(
                new Finding(FindingSeverity.Fatal, "BR-02", "The invoice carries no invoice number (BT-1).", Location: null, Path: "$.number", BusinessTerm: "BT-1"),
                Assert.Single(InvoiceRules.Validate(RuleDocuments.Ubl().Without("Invoice/ID").Input(), RuleSet.En16931, Catalog)));

        [Fact]
        public void Weighs_A_Finding_As_The_Syntax_Of_The_Invoice_Asks() =>
            Assert.Equal(
                FindingSeverity.Warning,
                Assert.Single(InvoiceRules.Validate(
                    RuleDocuments.Ubl().With("Invoice", "<cac:PaymentMeans><cbc:PaymentMeansCode>48</cbc:PaymentMeansCode><cac:CardAccount><cbc:PrimaryAccountNumberID>4111111111111111</cbc:PrimaryAccountNumberID></cac:CardAccount></cac:PaymentMeans>").Input(),
                    RuleSet.En16931,
                    Catalog)).Severity);

        [Fact]
        public void Weighs_A_Finding_As_The_Rule_Set_Of_The_Invoice_Asks() =>
            Assert.Equal(
                FindingSeverity.Information,
                Assert.Single(InvoiceRules.Validate(RuleDocuments.Ubl().Set("LegalMonetaryTotal/PayableAmount", "0.50").Input(), RuleSet.XRechnungExtension, Catalog)).Severity);

        [Fact]
        public void Checks_Only_The_Rules_Of_The_Rule_Set()
        {
            var catalog = new RuleCatalog([Catalog.Rules.Single(rule => rule.Id == "BR-02") with { RuleSets = new HashSet<RuleSet> { RuleSet.XRechnung } }]);

            Assert.Empty(InvoiceRules.Validate(RuleDocuments.Ubl().Without("Invoice/ID").Input(), RuleSet.En16931, catalog));
        }

        [Fact]
        public void Refuses_A_Catalog_That_Lists_A_Rule_The_Module_Does_Not_Implement()
        {
            var catalog = new RuleCatalog([new RuleDefinition("BR-99", "Group", "Standard", null, FindingSeverity.Fatal, null, null, "Message", new HashSet<RuleSet> { RuleSet.En16931 })]);

            Assert.Throws<InvalidOperationException>(() => InvoiceRules.Validate(RuleDocuments.Ubl().Input(), RuleSet.En16931, catalog));
        }
    }
}
