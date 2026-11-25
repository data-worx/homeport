// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.Rules;
using Homeport.Modules.EInvoicing.Domain.RuleSets;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed class RuleCatalogTests
{
    public sealed class LoadEmbedded
    {
        // The rule IDs `BR-` and two digits of the EN 16931 validation
        // artefacts, tag validation-1.3.17, in both preprocessed schematrons.
        private static readonly string[] CoreRuleIds =
        [
            "BR-01", "BR-02", "BR-03", "BR-04", "BR-05", "BR-06", "BR-07", "BR-08", "BR-09", "BR-10",
            "BR-11", "BR-12", "BR-13", "BR-14", "BR-15", "BR-16", "BR-17", "BR-18", "BR-19", "BR-20",
            "BR-21", "BR-22", "BR-23", "BR-24", "BR-25", "BR-26", "BR-27", "BR-28", "BR-29", "BR-30",
            "BR-31", "BR-32", "BR-33", "BR-36", "BR-37", "BR-38", "BR-41", "BR-42", "BR-43", "BR-44",
            "BR-45", "BR-46", "BR-47", "BR-48", "BR-49", "BR-50", "BR-51", "BR-52", "BR-53", "BR-54",
            "BR-55", "BR-56", "BR-57", "BR-61", "BR-62", "BR-63", "BR-64", "BR-65",
        ];

        private static readonly RuleCatalog Catalog = RuleCatalog.LoadEmbedded();

        [Fact]
        public void Lists_The_Core_Rules_Of_The_Validation_Artefacts() =>
            Assert.Equal(CoreRuleIds, Catalog.Rules.Where(rule => rule.Group == "EN 16931 core rules").Select(rule => rule.Id));

        [Fact]
        public void Lists_Only_Rules_The_Module_Implements() =>
            Assert.Empty(Catalog.Rules.Select(rule => rule.Id).Except(InvoiceRules.Implementations.Keys));

        [Fact]
        public void Implements_Only_Rules_It_Lists() =>
            Assert.Empty(InvoiceRules.Implementations.Keys.Except(Catalog.Rules.Select(rule => rule.Id)));

        [Fact]
        public void Checks_The_Core_Rules_Under_Every_Rule_Set() =>
            Assert.All(Catalog.Rules, rule => Assert.Equal(Enum.GetValues<RuleSet>().ToHashSet(), rule.RuleSets));

        [Fact]
        public void Takes_The_Core_Rules_From_A_Standard_The_Module_Declares() =>
            Assert.All(Catalog.Rules, rule => Assert.Equal("EN 16931 validation artefacts", rule.Standard));

        [Fact]
        public void Weighs_BR_51_As_A_Warning_In_Ubl_And_Fatal_In_Cii()
        {
            var rule = Catalog.Rules.Single(rule => rule.Id == "BR-51");

            Assert.Equal(
                (FindingSeverity.Warning, FindingSeverity.Warning, FindingSeverity.Fatal),
                (rule.SeverityIn(InvoiceSyntax.UblInvoice), rule.SeverityIn(InvoiceSyntax.UblCreditNote), rule.SeverityIn(InvoiceSyntax.Cii)));
        }

        [Fact]
        public void Names_A_Business_Term_For_Every_Core_Rule() =>
            Assert.All(Catalog.Rules, rule => Assert.Matches("^B[TG]-[0-9]+$", rule.BusinessTerm));
    }

    public sealed class Constructor
    {
        [Fact]
        public void Refuses_A_Rule_Listed_Twice()
        {
            var rule = new RuleDefinition("BR-01", "Group", "Standard", "BT-24", FindingSeverity.Fatal, null, null, "Message", new HashSet<RuleSet>());

            Assert.Throws<ArgumentException>(() => new RuleCatalog([rule, rule]));
        }
    }
}
