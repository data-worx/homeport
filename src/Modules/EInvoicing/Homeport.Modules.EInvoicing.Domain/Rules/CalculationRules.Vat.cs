// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;
using static Homeport.Modules.EInvoicing.Domain.Rules.XPathNumbers;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The calculation rules on VAT: the VAT point date, the VAT category of a line and the VAT breakdown (BG-23).</summary>
public static partial class CalculationRules
{
    // The tolerance BR-CO-17 allows between the stated and the computed tax amount.
    private const decimal TaxAmountTolerance = 1m;

    /// <summary>BR-CO-03: the invoice carries a VAT point date (BT-7) or a VAT point date code (BT-8), not both.</summary>
    /// <remarks>
    /// UBL checks the invoice once; CII checks it once for every VAT
    /// breakdown <see cref="RuleContexts"/> names, and looks for the two
    /// anywhere in the document.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo03(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        const string path = "$.vatPointDate";
        if (!input.IsCii)
        {
            return BrokenAt(
                input.Root.Elements(Cbc + "TaxPointDate").Any() && input.Root.Elements(Cac + "InvoicePeriod").Elements(Cbc + "DescriptionCode").Any(),
                path);
        }

        var broken = input.Root.Descendants(Ram + "TaxPointDate").Any() && input.Root.Descendants(Ram + "DueDateTypeCode").Any();

        return broken ? RuleContexts.VatBreakdowns(input).Select(_ => path) : [];
    }

    /// <summary>BR-CO-04: every invoice line carries the VAT category code of the invoiced item (BT-151).</summary>
    /// <remarks>Checked in the document: the code counts only under the tax scheme VAT, which the model does not keep.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo04(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.Lines
            .Select((line, index) => (line, index))
            .Where(entry => !(input.IsCii
                ? entry.line.Elements(Ram + "SpecifiedLineTradeSettlement").Elements(Ram + "ApplicableTradeTax").Where(IsCiiVat).Elements(Ram + "CategoryCode").Any()
                : entry.line.Elements(Cac + "Item").Elements(Cac + "ClassifiedTaxCategory").Where(IsUblVat).Elements(Cbc + "ID").Any()))
            .Select(entry => RulePaths.Line(entry.index) + ".vatInformation.vatCategoryCode");
    }

    /// <summary>BR-CO-17: the VAT category tax amount (BT-117) is the taxable amount (BT-116) times the VAT rate (BT-119), rounded to two decimals, within one unit either way.</summary>
    /// <remarks>
    /// A rate that rounds to 0, or no rate under VAT, asks for a tax amount
    /// that rounds to 0. UBL keeps the tolerance open, CII closed: a
    /// difference of exactly one unit breaks the rule in UBL only.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo17(RuleInput input) =>
        RuleContexts.VatBreakdowns(input)
            .Where(context => !(input.IsCii ? CiiTaxAmountAddsUp(context.Element) : UblTaxAmountAddsUp(context.Element)))
            .Select(context => context.PathTo("taxAmount"));

    /// <summary>BR-CO-18: the invoice carries at least one VAT breakdown (BG-23).</summary>
    /// <remarks>UBL checks the invoice once; CII checks it once for every invoice line.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo18(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        const string path = "$.vatBreakdowns";
        if (!input.IsCii)
        {
            return BrokenAt(!input.Root.Elements(Cac + "TaxTotal").Elements(Cac + "TaxSubtotal").Any(), path);
        }

        var hasBreakdown = input.Root.Descendants(Rsm + "SupplyChainTradeTransaction").Elements(Ram + "ApplicableHeaderTradeSettlement").Elements(Ram + "ApplicableTradeTax").Any();

        return hasBreakdown ? [] : input.Lines.Select(_ => path);
    }

    /// <summary>Whether the tax amount of a UBL subtotal adds up.</summary>
    private static bool UblTaxAmountAddsUp(XElement subtotal)
    {
        var rate = Decimal(subtotal.Elements(Cac + "TaxCategory").Where(IsUblVat).Elements(Cbc + "Percent").FirstOrDefault());
        var tax = Decimal(subtotal.Element(Cbc + "TaxAmount"));
        var expected = ExpectedTaxAmount(Decimal(subtotal.Element(Cbc + "TaxableAmount")), rate);

        return (AreEqual(Round(rate), 0m) && AreEqual(Round(tax), 0m))
            || (Round(rate) is { } rounded && rounded != 0m
                && IsLess(Subtract(Abs(tax), TaxAmountTolerance), expected)
                && IsLess(expected, Add(Abs(tax), TaxAmountTolerance)))
            || (rate is null && AreEqual(Round(tax), 0m));
    }

    /// <summary>Whether the tax amount of a CII header trade tax adds up; its rate counts only for a tax of the type VAT.</summary>
    private static bool CiiTaxAmountAddsUp(XElement tax)
    {
        var isVat = NormalizeSpace(tax.Element(Ram + "TypeCode")?.Value.ToUpperInvariant()) == VatScheme;
        var rate = isVat ? Decimal(tax.Element(Ram + "RateApplicablePercent")) : null;
        var amount = Decimal(tax.Element(Ram + "CalculatedAmount"));
        var expected = ExpectedTaxAmount(Decimal(tax.Element(Ram + "BasisAmount")), rate);

        return (AreEqual(Round(rate), 0m) && AreEqual(Round(amount), 0m))
            || (Round(rate) is { } rounded && rounded != 0m
                && IsLessOrEqual(Subtract(Abs(amount), TaxAmountTolerance), expected)
                && IsLessOrEqual(expected, Add(Abs(amount), TaxAmountTolerance)))
            || (rate is null && AreEqual(Round(amount), 0m));
    }

    /// <summary>The tax amount the schematron computes: the absolute taxable amount times the rate in percent, rounded to two decimals.</summary>
    private static decimal? ExpectedTaxAmount(decimal? taxableAmount, decimal? rate) => RoundToCents(Multiply(Abs(taxableAmount), Hundredth(rate)));
}
