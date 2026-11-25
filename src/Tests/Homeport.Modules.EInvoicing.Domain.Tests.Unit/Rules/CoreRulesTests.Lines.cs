// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Rules;

namespace Homeport.Modules.EInvoicing.Domain.Tests.Unit.Rules;

public sealed partial class CoreRulesTests
{
    public sealed class Br21
    {
        [Fact]
        public void Holds_With_A_Line_Identifier() => Assert.Empty(CoreRules.Br21(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_A_Line_Identifier() =>
            Assert.Equal(["$.lines[0].identifier"], CoreRules.Br21(RuleDocuments.Ubl().Without("InvoiceLine/ID").Input()));
    }

    public sealed class Br22
    {
        [Fact]
        public void Holds_With_An_Invoiced_Quantity() => Assert.Empty(CoreRules.Br22(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_Without_An_Invoiced_Quantity() =>
            Assert.Equal(["$.lines[0].invoicedQuantity"], CoreRules.Br22(RuleDocuments.Cii().Without("SpecifiedLineTradeDelivery/BilledQuantity").Input()));
    }

    public sealed class Br23
    {
        [Fact]
        public void Holds_With_A_Blank_Unit_The_Model_Drops() =>
            Assert.Empty(CoreRules.Br23(RuleDocuments.Ubl().Without("InvoiceLine/InvoicedQuantity").With("InvoiceLine", "<cbc:InvoicedQuantity unitCode=\" \">1</cbc:InvoicedQuantity>").Input()));

        [Fact]
        public void Breaks_Without_A_Ubl_Unit() =>
            Assert.Equal(["$.lines[0].invoicedQuantityUnitCode"], CoreRules.Br23(RuleDocuments.Ubl().Without("InvoiceLine/InvoicedQuantity").With("InvoiceLine", "<cbc:InvoicedQuantity>1</cbc:InvoicedQuantity>").Input()));

        [Fact]
        public void Breaks_Without_A_Cii_Unit() =>
            Assert.Equal(["$.lines[0].invoicedQuantityUnitCode"], CoreRules.Br23(RuleDocuments.Cii().Without("SpecifiedLineTradeDelivery/BilledQuantity").With("SpecifiedLineTradeDelivery", "<ram:BilledQuantity>1</ram:BilledQuantity>").Input()));
    }

    public sealed class Br24
    {
        [Fact]
        public void Holds_With_A_Net_Amount() => Assert.Empty(CoreRules.Br24(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_A_Net_Amount() =>
            Assert.Equal(["$.lines[0].netAmount"], CoreRules.Br24(RuleDocuments.Ubl().Without("InvoiceLine/LineExtensionAmount").Input()));
    }

    public sealed class Br25
    {
        [Fact]
        public void Holds_With_An_Item_Name() => Assert.Empty(CoreRules.Br25(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_With_A_Blank_Item_Name() =>
            Assert.Equal(["$.lines[0].item.name"], CoreRules.Br25(RuleDocuments.Cii().Set("SpecifiedTradeProduct/Name", " ").Input()));
    }

    public sealed class Br26
    {
        [Fact]
        public void Holds_With_A_Net_Price() => Assert.Empty(CoreRules.Br26(RuleDocuments.Cii().Input()));

        [Fact]
        public void Breaks_Without_A_Net_Price() =>
            Assert.Equal(["$.lines[0].priceDetails.netPrice"], CoreRules.Br26(RuleDocuments.Ubl().Without("InvoiceLine/Price").Input()));
    }

    public sealed class Br27
    {
        [Fact]
        public void Holds_With_A_Net_Price_Of_0() => Assert.Empty(CoreRules.Br27(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Breaks_With_A_Negative_Net_Price() =>
            Assert.Equal(["$.lines[0].priceDetails.netPrice"], CoreRules.Br27(RuleDocuments.Cii().Set("NetPriceProductTradePrice/ChargeAmount", "-1").Input()));

        [Fact]
        public void Breaks_Without_A_Net_Price() =>
            Assert.Equal(["$.lines[0].priceDetails.netPrice"], CoreRules.Br27(RuleDocuments.Ubl().Without("InvoiceLine/Price").Input()));
    }

    public sealed class Br28
    {
        [Fact]
        public void Holds_Without_A_Gross_Price() => Assert.Empty(CoreRules.Br28(RuleDocuments.Ubl().Input()));

        [Fact]
        public void Holds_When_One_Of_Several_Ubl_Base_Amounts_Is_Not_Negative() =>
            Assert.Empty(CoreRules.Br28(RuleDocuments.Ubl().With("InvoiceLine/Price", "<cac:AllowanceCharge><cbc:ChargeIndicator>false</cbc:ChargeIndicator><cbc:Amount currencyID=\"EUR\">0</cbc:Amount><cbc:BaseAmount currencyID=\"EUR\">-1</cbc:BaseAmount></cac:AllowanceCharge><cac:AllowanceCharge><cbc:ChargeIndicator>false</cbc:ChargeIndicator><cbc:Amount currencyID=\"EUR\">0</cbc:Amount><cbc:BaseAmount currencyID=\"EUR\">5</cbc:BaseAmount></cac:AllowanceCharge>").Input()));

        [Fact]
        public void Breaks_With_A_Negative_Ubl_Base_Amount() =>
            Assert.Equal(["$.lines[0].priceDetails.grossPrice"], CoreRules.Br28(RuleDocuments.Ubl().With("InvoiceLine/Price", "<cac:AllowanceCharge><cbc:ChargeIndicator>false</cbc:ChargeIndicator><cbc:Amount currencyID=\"EUR\">0</cbc:Amount><cbc:BaseAmount currencyID=\"EUR\">-1</cbc:BaseAmount></cac:AllowanceCharge>").Input()));

        [Fact]
        public void Breaks_With_A_Negative_Cii_Gross_Price() =>
            Assert.Equal(["$.lines[0].priceDetails.grossPrice"], CoreRules.Br28(RuleDocuments.Cii().With("SpecifiedLineTradeAgreement", "<ram:GrossPriceProductTradePrice><ram:ChargeAmount>-1</ram:ChargeAmount></ram:GrossPriceProductTradePrice>").Input()));
    }

    public sealed class Br30
    {
        [Fact]
        public void Holds_When_The_Line_Period_Ends_After_Its_Start() =>
            Assert.Empty(CoreRules.Br30(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", "<ram:BillingSpecifiedPeriod><ram:StartDateTime><udt:DateTimeString format=\"102\">20261101</udt:DateTimeString></ram:StartDateTime><ram:EndDateTime><udt:DateTimeString format=\"102\">20261130</udt:DateTimeString></ram:EndDateTime></ram:BillingSpecifiedPeriod>").Input()));

        [Fact]
        public void Holds_When_Ubl_Dates_With_Offsets_Compare_In_Order() =>
            Assert.Empty(CoreRules.Br30(RuleDocuments.Ubl().With("InvoiceLine", "<cac:InvoicePeriod><cbc:StartDate>2026-11-01+01:00</cbc:StartDate><cbc:EndDate>2026-11-01Z</cbc:EndDate></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Breaks_When_The_Ubl_Line_Period_Ends_Before_Its_Start() =>
            Assert.Equal(["$.lines[0].period"], CoreRules.Br30(RuleDocuments.Ubl().With("InvoiceLine", "<cac:InvoicePeriod><cbc:StartDate>2026-11-30</cbc:StartDate><cbc:EndDate>2026-11-01</cbc:EndDate></cac:InvoicePeriod>").Input()));

        [Fact]
        public void Breaks_When_The_Cii_Line_Period_Ends_Before_Its_Start() =>
            Assert.Equal(["$.lines[0].period"], CoreRules.Br30(RuleDocuments.Cii().With("SpecifiedLineTradeSettlement", "<ram:BillingSpecifiedPeriod><ram:StartDateTime><udt:DateTimeString format=\"102\">20261130</udt:DateTimeString></ram:StartDateTime><ram:EndDateTime><udt:DateTimeString format=\"102\">20261101</udt:DateTimeString></ram:EndDateTime></ram:BillingSpecifiedPeriod>").Input()));
    }

    public sealed class Br54
    {
        [Fact]
        public void Holds_For_An_Attribute_With_A_Name_And_A_Value() =>
            Assert.Empty(CoreRules.Br54(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:AdditionalItemProperty><cbc:Name>Colour</cbc:Name><cbc:Value>Blue</cbc:Value></cac:AdditionalItemProperty>").Input()));

        [Fact]
        public void Holds_For_A_Blank_Value_The_Model_Drops() =>
            Assert.Empty(CoreRules.Br54(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:ApplicableProductCharacteristic><ram:Description>Colour</ram:Description><ram:Value> </ram:Value></ram:ApplicableProductCharacteristic>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Attribute_Without_A_Value() =>
            Assert.Equal(["$.lines[0].item.attributes[1]"], CoreRules.Br54(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:AdditionalItemProperty><cbc:Name>Colour</cbc:Name><cbc:Value>Blue</cbc:Value></cac:AdditionalItemProperty><cac:AdditionalItemProperty><cbc:Name>Size</cbc:Name></cac:AdditionalItemProperty>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Attribute_Without_A_Name() =>
            Assert.Equal(["$.lines[0].item.attributes[0]"], CoreRules.Br54(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:ApplicableProductCharacteristic><ram:Value>Blue</ram:Value></ram:ApplicableProductCharacteristic>").Input()));
    }

    public sealed class Br64
    {
        [Fact]
        public void Holds_For_A_Standard_Identifier_With_A_Scheme() =>
            Assert.Empty(CoreRules.Br64(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:StandardItemIdentification><cbc:ID schemeID=\"0160\">4012345678901</cbc:ID></cac:StandardItemIdentification>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Standard_Identifier_Without_A_Scheme() =>
            Assert.Equal(["$.lines[0].item.standardIdentifier"], CoreRules.Br64(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:StandardItemIdentification><cbc:ID>4012345678901</cbc:ID></cac:StandardItemIdentification>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Global_Identifier_With_A_Blank_Scheme() =>
            Assert.Equal(["$.lines[0].item.standardIdentifier"], CoreRules.Br64(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:GlobalID schemeID=\"\">4012345678901</ram:GlobalID>").Input()));
    }

    public sealed class Br65
    {
        [Fact]
        public void Holds_For_A_Classification_With_A_Code_List() =>
            Assert.Empty(CoreRules.Br65(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:DesignatedProductClassification><ram:ClassCode listID=\"STI\">12345</ram:ClassCode></ram:DesignatedProductClassification>").Input()));

        [Fact]
        public void Holds_For_A_Cii_Classification_Without_A_Code() =>
            Assert.Empty(CoreRules.Br65(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:DesignatedProductClassification/>").Input()));

        [Fact]
        public void Breaks_For_A_Ubl_Classification_Without_A_Code_List() =>
            Assert.Equal(["$.lines[0].item.classificationIdentifiers[0]"], CoreRules.Br65(RuleDocuments.Ubl().With("InvoiceLine/Item", "<cac:CommodityClassification><cbc:ItemClassificationCode>12345</cbc:ItemClassificationCode></cac:CommodityClassification>").Input()));

        [Fact]
        public void Breaks_For_A_Cii_Classification_Without_A_Code_List() =>
            Assert.Equal(["$.lines[0].item.classificationIdentifiers[0]"], CoreRules.Br65(RuleDocuments.Cii().With("SpecifiedTradeProduct", "<ram:DesignatedProductClassification><ram:ClassCode>12345</ram:ClassCode></ram:DesignatedProductClassification>").Input()));
    }
}
