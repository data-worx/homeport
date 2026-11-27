// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The code-list rules on items, quantities, VAT categories and the reasons of allowances and charges.</summary>
public static partial class CodeListRules
{
    /// <summary>BR-CL-13: the scheme of an item classification identifier (BT-158) is a code of UNTDID 7143.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl13(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var codes = input.IsCii
            ? input.Root.Descendants(Ram + "ClassCode")
            : Below(input, Cac + "CommodityClassification", Cbc + "ItemClassificationCode");

        return NotCoded(codes.WithAttribute(ListIdAttribute), ListIdAttribute, CodeLists.Untdid7143, input.LinePathOf);
    }

    /// <summary>BR-CL-15: the item country of origin (BT-159) is a code of ISO 3166-1.</summary>
    /// <remarks>
    /// CII leaves out an identifier of the origin country with a scheme:
    /// BR-CL-11 comes before the rule in its pattern and checks it instead.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl15(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var codes = input.IsCii
            ? Below(input, Ram + "OriginTradeCountry", Ram + "ID").Where(code => code.Attribute(SchemeIdAttribute) is null)
            : Below(input, Cac + "OriginCountry", Cbc + "IdentificationCode");

        return NotCoded(codes, CodeLists.Iso3166, input.LinePathOf);
    }

    /// <summary>BR-CL-17: the VAT category code of a tax category is a code of UNTDID 5305.</summary>
    /// <remarks>
    /// UBL checks every tax category — of a VAT breakdown (BT-118) and of a
    /// document level allowance or charge (BT-95, BT-102); CII the category
    /// trade taxes of the allowances and charges.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl17(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var codes = input.IsCii
            ? Below(input, Ram + "CategoryTradeTax", Ram + "CategoryCode")
            : Below(input, Cac + "TaxCategory", Cbc + "ID");

        return NotCoded(codes, CodeLists.Untdid5305, input.LinePathOf);
    }

    /// <summary>BR-CL-18: the VAT category code of a classified or applicable tax is a code of UNTDID 5305.</summary>
    /// <remarks>
    /// UBL checks the classified tax category of every item (BT-151); CII
    /// every applicable trade tax — of the VAT breakdown (BT-118) and of an
    /// invoice line (BT-151).
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl18(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var codes = input.IsCii
            ? Below(input, Ram + "ApplicableTradeTax", Ram + "CategoryCode")
            : Below(input, Cac + "ClassifiedTaxCategory", Cbc + "ID");

        return NotCoded(codes, CodeLists.Untdid5305, input.LinePathOf);
    }

    /// <summary>BR-CL-19: the reason code of an allowance (BT-98, BT-140) is a code of UNTDID 5189.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl19(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return NotCoded(ReasonCodes(input, isCharge: false), CodeLists.Untdid5189, input.LinePathOf);
    }

    /// <summary>BR-CL-20: the reason code of a charge (BT-105, BT-145) is a code of UNTDID 7161.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl20(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return NotCoded(ReasonCodes(input, isCharge: true), CodeLists.Untdid7161, input.LinePathOf);
    }

    /// <summary>BR-CL-21: the scheme of an item standard identifier (BT-157) is a code of ISO 6523 ICD.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl21(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var identifiers = input.IsCii
            ? Below(input, Ram + "SpecifiedTradeProduct", Ram + "GlobalID")
            : Below(input, Cac + "StandardItemIdentification", Cbc + "ID");

        return NotCoded(identifiers.WithAttribute(SchemeIdAttribute), SchemeIdAttribute, CodeLists.Iso6523Icd, input.LinePathOf);
    }

    /// <summary>BR-CL-23: the unit of a quantity (BT-130, BT-150) is a code of UNECE Recommendations 20 and 21.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCl23(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        XName[] quantities = input.IsCii
            ? [Ram + "BasisQuantity", Ram + "BilledQuantity"]
            : [Cbc + "InvoicedQuantity", Cbc + "BaseQuantity", Cbc + "CreditedQuantity"];

        return NotCoded(
            input.Root.Descendants().Where(element => quantities.Contains(element.Name)).WithAttribute(UnitCodeAttribute),
            UnitCodeAttribute,
            CodeLists.UnitsOfMeasure,
            input.LinePathOf);
    }

    /// <summary>The reason codes of every allowance or, with <paramref name="isCharge"/>, every charge, its indicator compared as an XML Schema boolean.</summary>
    private static IEnumerable<XElement> ReasonCodes(RuleInput input, bool isCharge) => input.IsCii
        ? input.Root.Descendants(Ram + "SpecifiedTradeAllowanceCharge")
            .Where(entry => entry.Elements(Ram + "ChargeIndicator").Elements(Udt + "Indicator").Any(indicator => IsBoolean(indicator, isCharge)))
            .Elements(Ram + "ReasonCode")
        : input.Root.Descendants(Cac + "AllowanceCharge")
            .Where(entry => entry.Elements(Cbc + "ChargeIndicator").Any(indicator => IsBoolean(indicator, isCharge)))
            .Elements(Cbc + "AllowanceChargeReasonCode");
}
