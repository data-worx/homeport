// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The core rules on the invoice as a whole: its identification, its totals and its invoicing period.</summary>
public static partial class CoreRules
{
    /// <summary>BR-01: the invoice carries a specification identifier (BT-24).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br01(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.ProcessControl?.SpecificationIdentifier is null, "$.processControl.specificationIdentifier");
    }

    /// <summary>BR-02: the invoice carries an invoice number (BT-1).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br02(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.Number is null, "$.number");
    }

    /// <summary>BR-03: the invoice carries an issue date (BT-2).</summary>
    /// <remarks>
    /// Checked in the document: the model holds no date it cannot read,
    /// while the schematron asks only for a value — in CII one in format 102.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br03(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var date = input.IsCii
            ? input.Root.Element(Rsm + "ExchangedDocument")?.Element(Ram + "IssueDateTime")?.Elements(Udt + "DateTimeString")
                .FirstOrDefault(value => value.Attribute(FormatAttribute)?.Value == DateFormat102)
            : input.Root.Element(Cbc + "IssueDate");

        return BrokenAt(IsBlank(date), "$.issueDate");
    }

    /// <summary>BR-04: the invoice carries an invoice type code (BT-3).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br04(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.TypeCode is null, "$.typeCode");
    }

    /// <summary>BR-05: the invoice carries an invoice currency code (BT-5).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br05(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.CurrencyCode is null, "$.currencyCode");
    }

    /// <summary>BR-12: the document totals carry the sum of the line net amounts (BT-106).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br12(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.DocumentTotals is { SumOfLineNetAmounts: null }, "$.documentTotals.sumOfLineNetAmounts");
    }

    /// <summary>BR-13: the document totals carry the total amount without VAT (BT-109).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br13(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.DocumentTotals is { TotalWithoutVat: null }, "$.documentTotals.totalWithoutVat");
    }

    /// <summary>BR-14: the document totals carry the total amount with VAT (BT-112).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br14(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.DocumentTotals is { TotalWithVat: null }, "$.documentTotals.totalWithVat");
    }

    /// <summary>BR-15: the document totals carry the amount due for payment (BT-115).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br15(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.DocumentTotals is { AmountDueForPayment: null }, "$.documentTotals.amountDueForPayment");
    }

    /// <summary>BR-16: the invoice has at least one invoice line (BG-25).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br16(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(input.Invoice.Lines.Count == 0, "$.lines");
    }

    /// <summary>BR-29: the invoicing period ends on or after its start (BT-73, BT-74).</summary>
    /// <remarks>
    /// Checked in the document. UBL matches every <c>cac:InvoicePeriod</c>,
    /// a line's included, and compares the dates as xs:date; CII matches the
    /// period of the header and compares the values in format 102 as text,
    /// so a date in another format breaks the rule.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br29(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsCii)
        {
            var period = input.Root.Element(Rsm + "SupplyChainTradeTransaction")?.Element(Ram + "ApplicableHeaderTradeSettlement")
                ?.Elements(Ram + "BillingSpecifiedPeriod") ?? [];

            return period.Where(IsCiiPeriodBroken).Select(_ => "$.invoicingPeriod");
        }

        return input.Root.Descendants(Cac + "InvoicePeriod")
            .Where(IsUblPeriodBroken)
            .Select(period => input.LinePathOf(period) is var line && line == RulePaths.Invoice ? "$.invoicingPeriod" : $"{line}.period");
    }

    /// <summary>BR-53: a VAT accounting currency code (BT-6) comes with the total VAT amount in that currency (BT-111).</summary>
    /// <remarks>
    /// Checked in the document, where the tax totals carry their currency.
    /// UBL asks for a tax total in each accounting currency; CII asks for it in
    /// the document totals and, beyond that, for an accounting currency that
    /// differs from the invoice currency.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br53(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        const string path = "$.documentTotals.totalVatAmountInAccountingCurrency";
        if (!input.IsCii)
        {
            var currencies = input.Root.Descendants(Cac + "TaxTotal").Elements(Cbc + "TaxAmount")
                .Select(amount => amount.Attribute(CurrencyIdAttribute)?.Value)
                .ToHashSet(StringComparer.Ordinal);

            return BrokenAt(input.Root.Elements(Cbc + "TaxCurrencyCode").Any(code => !currencies.Contains(code.Value)), path);
        }

        var settlement = input.Root.Element(Rsm + "SupplyChainTradeTransaction")?.Element(Ram + "ApplicableHeaderTradeSettlement");
        var accounting = settlement?.Elements(Ram + "TaxCurrencyCode").Select(code => code.Value).ToList() ?? [];
        var invoice = settlement?.Elements(Ram + "InvoiceCurrencyCode").Select(code => code.Value).ToList() ?? [];

        return input.Root.Descendants(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation")
            .Where(totals => accounting.Count > 0
                && !(totals.Elements(Ram + "TaxTotalAmount").Any(amount => amount.Attribute(CurrencyIdAttribute) is { } currencyId && accounting.Contains(currencyId.Value))
                    && !accounting.Any(invoice.Contains)))
            .Select(_ => path);
    }

    /// <summary>Whether a UBL period has both dates and ends before it starts.</summary>
    private static bool IsUblPeriodBroken(XElement period)
    {
        var ends = period.Elements(Cbc + "EndDate").ToList();
        var starts = period.Elements(Cbc + "StartDate").ToList();

        return ends.Count > 0 && starts.Count > 0 && !AnyDateOnOrAfter(ends, starts);
    }

    /// <summary>Whether a CII period has both dates and its end in format 102 does not compare on or after its start.</summary>
    private static bool IsCiiPeriodBroken(XElement period)
    {
        var ends = period.Elements(Ram + "EndDateTime").ToList();
        var starts = period.Elements(Ram + "StartDateTime").ToList();

        return ends.Count > 0 && starts.Count > 0 && !AnyStringOnOrAfter(InFormat102(ends), InFormat102(starts));
    }

    /// <summary>The values in format 102 of CII date-time elements.</summary>
    private static IEnumerable<string> InFormat102(IEnumerable<XElement> dateTimes) =>
        dateTimes.Elements(Udt + "DateTimeString").Where(value => value.Attribute(FormatAttribute)?.Value == DateFormat102).Select(value => value.Value);
}
