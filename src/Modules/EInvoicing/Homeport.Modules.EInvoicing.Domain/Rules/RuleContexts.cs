// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Reading;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The elements the schematron of EN 16931 checks a group of rules on, the
/// same for every rule of the group, with the paths their findings point at.
/// </summary>
/// <remarks>
/// A schematron pattern checks an element against the first of its rules
/// whose context matches it and against no later one. Where an earlier rule
/// of the pattern matches some of the elements, those are left out here, so
/// a rule is checked where the schematron checks it and nowhere else.
/// </remarks>
internal static class RuleContexts
{
    // The VAT categories whose CII header trade taxes the pattern checks
    // against a rule of their own category, before the rule of every VAT
    // breakdown; the comparison is exact, as XPath compares untyped text.
    private static readonly string[] CiiCategoriesCheckedApart = ["L", "M", "O"];

    /// <summary>The document level allowances or, with <paramref name="isCharge"/>, charges.</summary>
    /// <remarks>
    /// The schematron's indicator decides which are checked: UBL's as an XML
    /// Schema boolean, CII's as the exact text <c>false</c> or <c>true</c>.
    /// The position in the path counts them as the model does, so it names
    /// the entry the model holds.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    /// <param name="isCharge">Whether charges rather than allowances are meant.</param>
    public static IEnumerable<RuleContext> DocumentLevelAllowancesOrCharges(RuleInput input, bool isCharge)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parent = input.IsCii ? HeaderSettlement(input) : input.Root;

        return AllowancesOrCharges(input, parent, isCharge, isCharge ? "$.documentLevelCharges" : "$.documentLevelAllowances");
    }

    /// <summary>The allowances or, with <paramref name="isCharge"/>, charges of the invoice lines, as <see cref="DocumentLevelAllowancesOrCharges"/> tells them apart.</summary>
    /// <param name="input">The invoice.</param>
    /// <param name="isCharge">Whether charges rather than allowances are meant.</param>
    public static IEnumerable<RuleContext> LineLevelAllowancesOrCharges(RuleInput input, bool isCharge)
    {
        ArgumentNullException.ThrowIfNull(input);

        var list = isCharge ? ".charges" : ".allowances";

        return input.Lines.SelectMany((line, index) => AllowancesOrCharges(
            input,
            input.IsCii ? line.Element(Ram + "SpecifiedLineTradeSettlement") : line,
            isCharge,
            RulePaths.Line(index) + list));
    }

    /// <summary>The VAT breakdowns (BG-23).</summary>
    /// <remarks>
    /// UBL checks every <c>cac:TaxSubtotal</c> of a tax total, a line's
    /// included; a subtotal of a line points at the line. CII checks the trade
    /// taxes of the header but those of the categories L, M and O, which an
    /// earlier rule of the pattern checks.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<RuleContext> VatBreakdowns(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsCii)
        {
            return (HeaderSettlement(input)?.Elements(Ram + "ApplicableTradeTax") ?? [])
                .Select((tax, index) => new RuleContext(tax, RulePaths.Item("$.vatBreakdowns", index), InModel: true))
                .Where(context => !IsCheckedApart(context.Element));
        }

        // The model holds the subtotals of the invoice's own tax totals, in their order.
        var modelSubtotals = input.Root.Elements(Cac + "TaxTotal").Elements(Cac + "TaxSubtotal").ToList();

        return input.Root.Descendants(Cac + "TaxSubtotal")
            .Where(subtotal => subtotal.Parent?.Name == Cac + "TaxTotal")
            .Select(subtotal => IndexOf(modelSubtotals, subtotal) is var index and >= 0
                ? new RuleContext(subtotal, RulePaths.Item("$.vatBreakdowns", index), InModel: true)
                : new RuleContext(subtotal, input.LinePathOf(subtotal), InModel: false));
    }

    /// <summary>The invoicing periods (BG-14).</summary>
    /// <remarks>
    /// UBL checks every <c>cac:InvoicePeriod</c> but those of an invoice
    /// line, which an earlier rule of the pattern checks; CII checks the
    /// period of the header.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<RuleContext> InvoicingPeriods(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsCii)
        {
            return (HeaderSettlement(input)?.Elements(Ram + "BillingSpecifiedPeriod") ?? [])
                .Select(period => new RuleContext(period, "$.invoicingPeriod", InModel: true));
        }

        return input.Root.Descendants(Cac + "InvoicePeriod")
            .Where(period => period.Parent?.Name != Cac + "InvoiceLine" && period.Parent?.Name != Cac + "CreditNoteLine")
            .Select(period => input.LinePathOf(period) is var line && line == RulePaths.Invoice
                ? new RuleContext(period, "$.invoicingPeriod", InModel: true)
                : new RuleContext(period, line, InModel: false));
    }

    /// <summary>The document totals (BG-22): UBL's <c>cac:LegalMonetaryTotal</c>, CII's header monetary summation.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<RuleContext> DocumentTotals(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var totals = input.IsCii
            ? input.Root.Descendants(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation")
            : input.Root.Descendants(Cac + "LegalMonetaryTotal");

        return totals.Select(total => new RuleContext(total, "$.documentTotals", InModel: true));
    }

    /// <summary>The settlement of the CII header, where the totals, taxes and allowances of the document lie.</summary>
    /// <param name="input">The invoice, in CII.</param>
    public static XElement? HeaderSettlement(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.Root.Element(Rsm + "SupplyChainTradeTransaction")?.Element(Ram + "ApplicableHeaderTradeSettlement");
    }

    /// <summary>The allowances or charges directly below <paramref name="parent"/>, in the list at <paramref name="list"/>.</summary>
    private static IEnumerable<RuleContext> AllowancesOrCharges(RuleInput input, XElement? parent, bool isCharge, string list)
    {
        var entries = (parent?.Elements(input.IsCii ? Ram + "SpecifiedTradeAllowanceCharge" : Cac + "AllowanceCharge") ?? []).ToList();
        var modelEntries = entries.Where(entry => IsModelCharge(input, entry) == isCharge).ToList();

        return entries
            .Where(entry => IsRuleCharge(input, entry, isCharge))
            .Select(entry => IndexOf(modelEntries, entry) is var index and >= 0
                ? new RuleContext(entry, RulePaths.Item(list, index), InModel: true)
                : new RuleContext(entry, list, InModel: false));
    }

    /// <summary>Whether the schematron checks the entry as an allowance or, with <paramref name="isCharge"/>, as a charge.</summary>
    private static bool IsRuleCharge(RuleInput input, XElement entry, bool isCharge) => input.IsCii
        ? entry.Elements(Ram + "ChargeIndicator").Elements(Udt + "Indicator").Any(indicator => indicator.Value == (isCharge ? "true" : "false"))
        : IsBoolean(entry.Element(Cbc + "ChargeIndicator"), isCharge);

    /// <summary>Whether the model holds the entry as a charge rather than an allowance; none when it holds it as neither.</summary>
    private static bool? IsModelCharge(RuleInput input, XElement entry)
    {
        var indicator = input.IsCii ? entry.Element(Ram + "ChargeIndicator")?.Element(Udt + "Indicator") : entry.Element(Cbc + "ChargeIndicator");

        return XmlValues.IsTrue(indicator) ? true : XmlValues.IsFalse(indicator) ? false : null;
    }

    /// <summary>Whether an earlier rule of the pattern checks the CII header trade tax, as one of VAT of a category checked apart.</summary>
    private static bool IsCheckedApart(XElement tax) =>
        IsCiiVat(tax) && tax.Elements(Ram + "CategoryCode").Any(code => CiiCategoriesCheckedApart.Contains(code.Value, StringComparer.Ordinal));
}
