// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The core rules on the VAT breakdown (BG-23). They are checked in the
/// document: UBL matches every <c>cac:TaxSubtotal</c> of a tax total, a
/// line's included, and the category code and rate count only under the tax
/// scheme VAT, which the model does not keep.
/// </summary>
public static partial class CoreRules
{
    /// <summary>BR-45: every VAT breakdown carries the VAT category taxable amount (BT-116).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br45(RuleInput input) =>
        VatBreakdowns(input, (breakdown, isCii) => breakdown.Element(isCii ? Ram + "BasisAmount" : Cbc + "TaxableAmount") is not null, "taxableAmount");

    /// <summary>BR-46: every VAT breakdown carries the VAT category tax amount (BT-117).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br46(RuleInput input) =>
        VatBreakdowns(input, (breakdown, isCii) => breakdown.Element(isCii ? Ram + "CalculatedAmount" : Cbc + "TaxAmount") is not null, "taxAmount");

    /// <summary>BR-47: every VAT breakdown carries a VAT category code (BT-118).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br47(RuleInput input) =>
        VatBreakdowns(
            input,
            (breakdown, isCii) => isCii
                ? IsCiiVat(breakdown) && breakdown.Element(Ram + "CategoryCode") is not null
                : breakdown.Elements(Cac + "TaxCategory").Where(IsUblVat).Elements(Cbc + "ID").Any(),
            "categoryCode");

    /// <summary>BR-48: every VAT breakdown carries a VAT category rate (BT-119), unless its category is not subject to VAT.</summary>
    /// <remarks>UBL compares the category code trimmed, CII exactly.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br48(RuleInput input) =>
        VatBreakdowns(
            input,
            (breakdown, isCii) =>
            {
                if (isCii)
                {
                    return IsCiiVat(breakdown)
                        && (breakdown.Element(Ram + "RateApplicablePercent") is not null
                            || breakdown.Elements(Ram + "CategoryCode").Any(code => code.Value == NotSubjectToVat));
                }

                var categories = breakdown.Elements(Cac + "TaxCategory").Where(IsUblVat).ToList();

                return categories.Elements(Cbc + "Percent").Any()
                    || categories.Any(category => NormalizeSpace(category.Element(Cbc + "ID")?.Value) == NotSubjectToVat);
            },
            "rate");

    /// <summary>The paths of the VAT breakdowns that lack what <paramref name="has"/> asks for.</summary>
    private static IEnumerable<string> VatBreakdowns(RuleInput input, Func<XElement, bool, bool> has, string member)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsCii)
        {
            var taxes = (input.Root.Element(Rsm + "SupplyChainTradeTransaction")?.Element(Ram + "ApplicableHeaderTradeSettlement")
                ?.Elements(Ram + "ApplicableTradeTax") ?? []).ToList();

            return taxes.Select((tax, index) => (tax, index))
                .Where(entry => !has(entry.tax, true))
                .Select(entry => $"{RulePaths.Item("$.vatBreakdowns", entry.index)}.{member}");
        }

        // The model holds the subtotals of the invoice's own tax totals, in their order.
        var modelSubtotals = input.Root.Elements(Cac + "TaxTotal").Elements(Cac + "TaxSubtotal").ToList();

        return input.Root.Descendants(Cac + "TaxSubtotal")
            .Where(subtotal => subtotal.Parent?.Name == Cac + "TaxTotal" && !has(subtotal, false))
            .Select(subtotal => IndexOf(modelSubtotals, subtotal) is var index and >= 0
                ? $"{RulePaths.Item("$.vatBreakdowns", index)}.{member}"
                : input.LinePathOf(subtotal));
    }
}
