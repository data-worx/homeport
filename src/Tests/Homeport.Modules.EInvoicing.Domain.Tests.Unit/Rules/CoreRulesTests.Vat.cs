// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CoreRulesTests
{
    private const string UblVatBreakdown = "<cac:TaxTotal><cbc:TaxAmount currencyID=\"EUR\">0</cbc:TaxAmount><cac:TaxSubtotal><cbc:TaxableAmount currencyID=\"EUR\">0</cbc:TaxableAmount><cbc:TaxAmount currencyID=\"EUR\">0</cbc:TaxAmount><cac:TaxCategory><cbc:ID>S</cbc:ID><cbc:Percent>19</cbc:Percent><cac:TaxScheme><cbc:ID>VAT</cbc:ID></cac:TaxScheme></cac:TaxCategory></cac:TaxSubtotal></cac:TaxTotal>";
    private const string CiiHeaderTax = "ApplicableHeaderTradeSettlement/ApplicableTradeTax";

    public sealed class Br45
    {
        [Fact]
        public void Holds_With_A_Taxable_Amount() => Assert.Empty(CoreRules.Br45(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_Without_A_Taxable_Amount() =>
            Assert.Equal(["$.vatBreakdowns[0].taxableAmount"], CoreRules.Br45(RuleDocuments.Cii().Without($"{CiiHeaderTax}/BasisAmount").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Subtotal_Of_A_Line() =>
            Assert.Equal(["$.lines[0]"], CoreRules.Br45(RuleDocuments.Ubl().With("InvoiceLine", UblVatBreakdown).Without("InvoiceLine/TaxTotal/TaxSubtotal/TaxableAmount").Input()));

        [Theory]
        [InlineData("L")]
        [InlineData("M")]
        [InlineData("O")]
        public void Holds_For_A_Cii_Tax_Of_A_Category_A_Rule_Of_Its_Own_Checks(string category) =>
            Assert.Empty(CoreRules.Br45(RuleDocuments.Cii().Set($"{CiiHeaderTax}/CategoryCode", category).Without($"{CiiHeaderTax}/BasisAmount").Input()));
    }

    public sealed class Br46
    {
        [Fact]
        public void Holds_With_A_Tax_Amount() => Assert.Empty(CoreRules.Br46(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_A_Tax_Amount() =>
            Assert.Equal(["$.vatBreakdowns[0].taxAmount"], CoreRules.Br46(RuleDocuments.Ubl().Without("TaxSubtotal/TaxAmount").Input()));
    }

    public sealed class Br47
    {
        [Fact]
        public void Holds_With_A_Category_Code() => Assert.Empty(CoreRules.Br47(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Category_Under_Another_Tax_Scheme() =>
            Assert.Equal(["$.vatBreakdowns[0].categoryCode"], CoreRules.Br47(RuleDocuments.Ubl().Set("TaxSubtotal/TaxCategory/TaxScheme/ID", "GST").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Tax_Of_Another_Type() =>
            Assert.Equal(["$.vatBreakdowns[0].categoryCode"], CoreRules.Br47(RuleDocuments.Cii().Set($"{CiiHeaderTax}/TypeCode", "GST").Input()));
    }

    public sealed class Br48
    {
        [Fact]
        public void Holds_With_A_Rate() => Assert.Empty(CoreRules.Br48(RuleDocuments.Cii().Input()));

        [Fact]
        public void Holds_For_A_Ubl_Category_O_Written_With_Spaces_Without_A_Rate() =>
            Assert.Empty(CoreRules.Br48(RuleDocuments.Ubl().Set("TaxSubtotal/TaxCategory/ID", " O ").Without("TaxSubtotal/TaxCategory/Percent").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Category_O_Written_With_Spaces_Without_A_Rate() =>
            Assert.Equal(["$.vatBreakdowns[0].rate"], CoreRules.Br48(RuleDocuments.Cii().Set($"{CiiHeaderTax}/CategoryCode", " O ").Without($"{CiiHeaderTax}/RateApplicablePercent").Input()));

        [Fact]
        public void Breaks_Without_A_Rate() =>
            Assert.Equal(["$.vatBreakdowns[0].rate"], CoreRules.Br48(RuleDocuments.Ubl().Without("TaxSubtotal/TaxCategory/Percent").Input()));
    }
}
