// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CodeListRulesTests
{
    public sealed class BrCl13
    {
        [Fact]
        public void Holds_For_A_Scheme_Of_Untdid_7143() =>
            Assert.Empty(CodeListRules.BrCl13(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:CommodityClassification><cbc:ItemClassificationCode listID=\"HS\">1</cbc:ItemClassificationCode></cac:CommodityClassification>").Input()));

        [Fact]
        public void Breaks_For_A_Scheme_Outside_Untdid_7143() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl13(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:CommodityClassification><cbc:ItemClassificationCode listID=\"NOCODE\">1</cbc:ItemClassificationCode></cac:CommodityClassification>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Classification_Of_An_Unknown_Scheme() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl13(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:DesignatedProductClassification><ram:ClassCode listID=\"NOCODE\">1</ram:ClassCode></ram:DesignatedProductClassification>").Input()));
    }

    public sealed class BrCl15
    {
        [Fact]
        public void Holds_For_Germany() =>
            Assert.Empty(CodeListRules.BrCl15(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:OriginCountry><cbc:IdentificationCode>DE</cbc:IdentificationCode></cac:OriginCountry>").Input()));

        [Fact]
        public void Breaks_For_A_Code_Outside_Iso_3166() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl15(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:OriginTradeCountry><ram:ID>XX</ram:ID></ram:OriginTradeCountry>").Input()));

        [Fact]
        public void Leaves_A_Cii_Origin_Country_With_A_Scheme_To_BR_CL_11() =>
            Assert.Empty(CodeListRules.BrCl15(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:OriginTradeCountry><ram:ID schemeID=\"0088\">XX</ram:ID></ram:OriginTradeCountry>").Input()));
    }

    public sealed class BrCl17
    {
        [Fact]
        public void Holds_For_The_Standard_Rate() => Assert.Empty(CodeListRules.BrCl17(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Breakdown_Of_An_Unknown_Category() =>
            Assert.Equal(["$"], CodeListRules.BrCl17(RuleDocuments.Ubl().Set("TaxTotal/TaxSubtotal/TaxCategory/ID", "X").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Allowance_Of_An_Unknown_Category() =>
            Assert.Equal(["$"], CodeListRules.BrCl17(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", "<ram:SpecifiedTradeAllowanceCharge><ram:ChargeIndicator><udt:Indicator>false</udt:Indicator></ram:ChargeIndicator><ram:ActualAmount>0</ram:ActualAmount><ram:CategoryTradeTax><ram:TypeCode>VAT</ram:TypeCode><ram:CategoryCode>X</ram:CategoryCode></ram:CategoryTradeTax></ram:SpecifiedTradeAllowanceCharge>").Input()));

        [Fact]
        public void Leaves_A_Cii_Applicable_Trade_Tax_To_BR_CL_18() =>
            Assert.Empty(CodeListRules.BrCl17(RuleDocuments.Cii().Set("ApplicableHeaderTradeSettlement/ApplicableTradeTax/CategoryCode", "X").Input()));
    }

    public sealed class BrCl18
    {
        [Fact]
        public void Holds_For_The_Standard_Rate() => Assert.Empty(CodeListRules.BrCl18(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Item_Of_An_Unknown_Category() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl18(RuleDocuments.Ubl().Set("ClassifiedTaxCategory/ID", "X").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Line_Of_An_Unknown_Category() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl18(RuleDocuments.Cii().Set("SpecifiedLineTradeSettlement/ApplicableTradeTax/CategoryCode", "X").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Breakdown_Of_An_Unknown_Category() =>
            Assert.Equal(["$"], CodeListRules.BrCl18(RuleDocuments.Cii().Set("ApplicableHeaderTradeSettlement/ApplicableTradeTax/CategoryCode", "X").Input()));
    }

    public sealed class BrCl19
    {
        private const string UblEntry = "<cac:AllowanceCharge><cbc:ChargeIndicator>{0}</cbc:ChargeIndicator><cbc:AllowanceChargeReasonCode>{1}</cbc:AllowanceChargeReasonCode></cac:AllowanceCharge>";

        [Fact]
        public void Holds_For_A_Discount() =>
            Assert.Empty(CodeListRules.BrCl19(RuleDocuments.Ubl().With("Invoice", string.Format(UblEntry, "false", "95")).Input()));

        [Fact]
        public void Breaks_For_An_Allowance_Code_Outside_Untdid_5189() =>
            Assert.Equal(["$"], CodeListRules.BrCl19(RuleDocuments.Ubl().With("Invoice", string.Format(UblEntry, "false", "NOCODE")).Input()));

        [Fact]
        public void Reads_The_Indicator_As_An_Xml_Schema_Boolean() =>
            Assert.Equal(["$"], CodeListRules.BrCl19(RuleDocuments.Ubl().With("Invoice", string.Format(UblEntry, "0", "NOCODE")).Input()));

        [Fact]
        public void Leaves_A_Charge_To_BR_CL_20() =>
            Assert.Empty(CodeListRules.BrCl19(RuleDocuments.Ubl().With("Invoice", string.Format(UblEntry, "true", "NOCODE")).Input()));

        [Fact]
        public void Breaks_For_A_Cii_Line_Allowance_Code_Outside_Untdid_5189() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl19(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", "<ram:SpecifiedTradeAllowanceCharge><ram:ChargeIndicator><udt:Indicator>false</udt:Indicator></ram:ChargeIndicator><ram:ActualAmount>0</ram:ActualAmount><ram:ReasonCode>NOCODE</ram:ReasonCode></ram:SpecifiedTradeAllowanceCharge>").Input()));
    }

    public sealed class BrCl20
    {
        private const string CiiEntry = "<ram:SpecifiedTradeAllowanceCharge><ram:ChargeIndicator><udt:Indicator>{0}</udt:Indicator></ram:ChargeIndicator><ram:ActualAmount>0</ram:ActualAmount><ram:ReasonCode>{1}</ram:ReasonCode></ram:SpecifiedTradeAllowanceCharge>";

        [Fact]
        public void Holds_For_A_Charge_Code_Of_Untdid_7161() =>
            Assert.Empty(CodeListRules.BrCl20(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", string.Format(CiiEntry, "true", "AA")).Input()));

        [Fact]
        public void Breaks_For_A_Charge_Code_Outside_Untdid_7161() =>
            Assert.Equal(["$"], CodeListRules.BrCl20(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", string.Format(CiiEntry, "true", "95")).Input()));

        [Fact]
        public void Leaves_An_Allowance_To_BR_CL_19() =>
            Assert.Empty(CodeListRules.BrCl20(RuleDocuments.Cii().With("ApplicableHeaderTradeSettlement", string.Format(CiiEntry, "false", "NOCODE")).Input()));
    }

    public sealed class BrCl21
    {
        [Fact]
        public void Holds_For_A_Gtin() =>
            Assert.Empty(CodeListRules.BrCl21(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:StandardItemIdentification><cbc:ID schemeID=\"0160\">1</cbc:ID></cac:StandardItemIdentification>").Input()));

        [Fact]
        public void Breaks_For_A_Scheme_Outside_Iso_6523() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl21(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:StandardItemIdentification><cbc:ID schemeID=\"0000\">1</cbc:ID></cac:StandardItemIdentification>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Product_Of_An_Unknown_Scheme() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl21(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:GlobalID schemeID=\"0000\">1</ram:GlobalID>").Input()));
    }

    public sealed class BrCl23
    {
        [Fact]
        public void Holds_For_One_Piece() => Assert.Empty(CodeListRules.BrCl23(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_For_A_Unit_Outside_Recommendation_20() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl23(RuleDocuments.Ubl().SetAttribute("InvoicedQuantity", "unitCode", "QQQQ").Input()));

        [Fact]
        public void Breaks_For_A_Credited_Quantity_Of_An_Unknown_Unit() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl23(RuleDocuments.UblCreditNote().SetAttribute("CreditedQuantity", "unitCode", "QQQQ").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Billed_Quantity_Of_An_Unknown_Unit() =>
            Assert.Equal(["$.lines[0]"], CodeListRules.BrCl23(RuleDocuments.Cii().SetAttribute("BilledQuantity", "unitCode", "QQQQ").Input()));
    }
}
