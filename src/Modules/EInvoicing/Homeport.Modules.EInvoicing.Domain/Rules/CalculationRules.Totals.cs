// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;
using static Homeport.Modules.EInvoicing.Domain.Rules.XPathNumbers;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The calculation rules on the document totals (BG-22), checked on each
/// <c>cac:LegalMonetaryTotal</c> in UBL and each header monetary summation in
/// CII. Where CII may repeat an amount, the schematron takes the first.
/// </summary>
public static partial class CalculationRules
{
    /// <summary>BR-CO-10: the sum of invoice line net amounts (BT-106) is the sum of the line net amounts (BT-131), rounded to two decimals.</summary>
    /// <remarks>UBL sums the lines anywhere in the document, CII the lines of the transaction the totals belong to.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo10(RuleInput input) =>
        RuleContexts.DocumentTotals(input)
            .Where(context => !AreEqual(
                Decimal(context.Element.Element(input.IsCii ? Ram + "LineTotalAmount" : Cbc + "LineExtensionAmount")),
                RoundToCents(Sum(LineNetAmounts(input, context.Element)))))
            .Select(context => context.PathTo("sumOfLineNetAmounts"));

    /// <summary>BR-CO-11: the sum of allowances on document level (BT-107) is the sum of the document level allowance amounts (BT-92), and is left out when there are none.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo11(RuleInput input) => SumOfAllowancesOrCharges(input, isCharge: false, "AllowanceTotalAmount", "sumOfAllowances");

    /// <summary>BR-CO-12: the sum of charges on document level (BT-108) is the sum of the document level charge amounts (BT-99), and is left out when there are none.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo12(RuleInput input) => SumOfAllowancesOrCharges(input, isCharge: true, "ChargeTotalAmount", "sumOfCharges");

    /// <summary>BR-CO-13: the invoice total amount without VAT (BT-109) is the sum of line net amounts (BT-106) minus the sum of allowances (BT-107) plus the sum of charges (BT-108).</summary>
    /// <remarks>UBL compares with the sum of line net amounts as it is when there are neither allowances nor charges; CII rounds it.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo13(RuleInput input) =>
        RuleContexts.DocumentTotals(input)
            .Where(context => !(input.IsCii ? CiiTotalWithoutVatAddsUp(context.Element) : UblTotalWithoutVatAddsUp(context.Element)))
            .Select(context => context.PathTo("totalWithoutVat"));

    /// <summary>BR-CO-14: the invoice total VAT amount (BT-110) is the sum of the VAT category tax amounts (BT-117), rounded to two decimals.</summary>
    /// <remarks>
    /// UBL checks each tax total of the invoice that has subtotals; CII
    /// checks each total VAT amount in the invoice currency against the
    /// tax amounts of the header, compared as doubles.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo14(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        const string path = "$.documentTotals.totalVatAmount";
        if (!input.IsCii)
        {
            return input.Root.Elements(Cac + "TaxTotal")
                .Where(total => total.Elements(Cac + "TaxSubtotal").Any()
                    && !AreEqual(Decimal(total.Element(Cbc + "TaxAmount")), RoundToCents(Sum(total.Elements(Cac + "TaxSubtotal").Elements(Cbc + "TaxAmount")))))
                .Select(_ => path);
        }

        var settlement = RuleContexts.HeaderSettlement(input);
        var currencies = InvoiceCurrencies(settlement);
        var expected = RoundToCents(Sum(settlement?.Elements(Ram + "ApplicableTradeTax").Elements(Ram + "CalculatedAmount") ?? []));

        return input.Root.Descendants(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation").Elements(Ram + "TaxTotalAmount")
            .Where(amount => amount.Attribute(CurrencyIdAttribute) is { } currency && currencies.Contains(currency.Value) && !AnyEqualsAsDouble([amount], expected))
            .Select(_ => path);
    }

    /// <summary>BR-CO-15: the invoice total amount with VAT (BT-112) is the total without VAT (BT-109) plus the total VAT amount (BT-110) in the invoice currency, rounded to two decimals.</summary>
    /// <remarks>
    /// UBL asks for exactly one total VAT amount in the invoice currency. CII
    /// asks for that too, or for a total with VAT equal to the total without
    /// VAT.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo15(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return BrokenAt(!(input.IsCii ? CiiTotalWithVatAddsUp(input) : UblTotalWithVatAddsUp(input)), "$.documentTotals.totalWithVat");
    }

    /// <summary>BR-CO-16: the amount due for payment (BT-115) is the total with VAT (BT-112) minus the paid amount (BT-113) plus the rounding amount (BT-114).</summary>
    /// <remarks>UBL rounds both sides to two decimals; CII compares without rounding.</remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo16(RuleInput input) =>
        RuleContexts.DocumentTotals(input)
            .Where(context => !(input.IsCii ? CiiAmountDueAddsUp(context.Element) : UblAmountDueAddsUp(context.Element)))
            .Select(context => context.PathTo("amountDueForPayment"));

    /// <summary>The net amounts of the invoice lines the sum of line net amounts in <paramref name="totals"/> adds up.</summary>
    private static IEnumerable<XElement> LineNetAmounts(RuleInput input, XElement totals) => input.IsCii
        ? totals.Parent?.Parent?.Elements(Ram + "IncludedSupplyChainTradeLineItem").Elements(Ram + "SpecifiedLineTradeSettlement")
            .Elements(Ram + "SpecifiedTradeSettlementLineMonetarySummation").Elements(Ram + "LineTotalAmount") ?? []
        : input.Root.Descendants().Where(line => line.Name == Cac + "InvoiceLine" || line.Name == Cac + "CreditNoteLine").Elements(Cbc + "LineExtensionAmount");

    /// <summary>The paths of the document totals whose sum of allowances or charges does not add up.</summary>
    /// <remarks>
    /// UBL tells an allowance from a charge by its indicator as an XML Schema
    /// boolean and compares as decimals; CII does the same with its
    /// indicator, compares as doubles and sums the first amount of each.
    /// </remarks>
    private static IEnumerable<string> SumOfAllowancesOrCharges(RuleInput input, bool isCharge, string sumName, string member)
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.IsCii)
        {
            var entries = (RuleContexts.HeaderSettlement(input)?.Elements(Ram + "SpecifiedTradeAllowanceCharge") ?? [])
                .Where(entry => entry.Elements(Ram + "ChargeIndicator").Elements(Udt + "Indicator").Any(indicator => IsBoolean(indicator, isCharge)))
                .ToList();
            var expected = RoundToCents(Sum(entries.Select(entry => entry.Element(Ram + "ActualAmount")).OfType<XElement>()));

            return RuleContexts.DocumentTotals(input)
                .Where(context => context.Element.Elements(Ram + sumName).ToList() is var sums
                    && !((entries.Count == 0 && sums.Count == 0) || AnyEqualsAsDouble(sums, expected)))
                .Select(context => context.PathTo(member));
        }

        return RuleContexts.DocumentTotals(input)
            .Where(context =>
            {
                var entries = (context.Element.Parent?.Elements(Cac + "AllowanceCharge") ?? [])
                    .Where(entry => IsBoolean(entry.Element(Cbc + "ChargeIndicator"), isCharge))
                    .ToList();
                var sum = context.Element.Element(Cbc + sumName);

                return !(AreEqual(Decimal(sum), RoundToCents(Sum(entries.Elements(Cbc + "Amount")))) || (sum is null && entries.Count == 0));
            })
            .Select(context => context.PathTo(member));
    }

    /// <summary>Whether a UBL total without VAT adds up, compared as the alternative for the sums its totals carry asks.</summary>
    private static bool UblTotalWithoutVatAddsUp(XElement totals)
    {
        var lines = Decimal(totals.Element(Cbc + "LineExtensionAmount"));
        var allowances = Decimal(totals.Element(Cbc + "AllowanceTotalAmount"));
        var charges = Decimal(totals.Element(Cbc + "ChargeTotalAmount"));
        var total = Decimal(totals.Element(Cbc + "TaxExclusiveAmount"));
        var hasAllowances = totals.Element(Cbc + "AllowanceTotalAmount") is not null;
        var hasCharges = totals.Element(Cbc + "ChargeTotalAmount") is not null;

        return (hasCharges && hasAllowances && AreEqual(total, RoundToCents(Subtract(Add(lines, charges), allowances))))
            || (!hasCharges && hasAllowances && AreEqual(total, RoundToCents(Subtract(lines, allowances))))
            || (hasCharges && !hasAllowances && AreEqual(total, RoundToCents(Add(lines, charges))))
            || (!hasCharges && !hasAllowances && AreEqual(total, lines));
    }

    /// <summary>Whether a CII total without VAT adds up, in one of the alternatives its schematron allows.</summary>
    private static bool CiiTotalWithoutVatAddsUp(XElement totals)
    {
        var lines = Decimal(totals.Element(Ram + "LineTotalAmount"));
        var allowances = Decimal(totals.Element(Ram + "AllowanceTotalAmount"));
        var charges = Decimal(totals.Element(Ram + "ChargeTotalAmount"));
        var total = Decimal(totals.Element(Ram + "TaxBasisTotalAmount"));
        var hasAllowances = totals.Element(Ram + "AllowanceTotalAmount") is not null;
        var hasCharges = totals.Element(Ram + "ChargeTotalAmount") is not null;

        return AreEqual(total, RoundToCents(Add(Subtract(lines, allowances), charges)))
            || (AreEqual(total, RoundToCents(Subtract(lines, allowances))) && !hasCharges)
            || (AreEqual(total, RoundToCents(Add(lines, charges))) && !hasAllowances)
            || (AreEqual(total, RoundToCents(lines)) && !hasCharges && !hasAllowances);
    }

    /// <summary>Whether the UBL total with VAT adds up for every invoice currency.</summary>
    private static bool UblTotalWithVatAddsUp(RuleInput input)
    {
        var totals = input.Root.Element(Cac + "LegalMonetaryTotal");

        return input.Root.Elements(Cbc + "DocumentCurrencyCode").All(currency =>
        {
            var vat = input.Root.Elements(Cac + "TaxTotal").Elements(Cbc + "TaxAmount")
                .Where(amount => amount.Attribute(CurrencyIdAttribute)?.Value == currency.Value)
                .ToList();

            return vat.Count == 1
                && AreEqual(Decimal(totals?.Element(Cbc + "TaxInclusiveAmount")), RoundToCents(Add(Decimal(totals?.Element(Cbc + "TaxExclusiveAmount")), Decimal(vat[0]))));
        });
    }

    /// <summary>Whether the CII total with VAT adds up for every invoice currency.</summary>
    private static bool CiiTotalWithVatAddsUp(RuleInput input)
    {
        var settlement = RuleContexts.HeaderSettlement(input);
        var totals = settlement?.Elements(Ram + "SpecifiedTradeSettlementHeaderMonetarySummation").ToList() ?? [];
        var withVat = Decimal(totals.Elements(Ram + "GrandTotalAmount").FirstOrDefault());
        var withoutVat = Decimal(totals.Elements(Ram + "TaxBasisTotalAmount").FirstOrDefault());

        return (settlement?.Elements(Ram + "InvoiceCurrencyCode") ?? []).All(currency =>
        {
            var vat = totals.Elements(Ram + "TaxTotalAmount")
                .Where(amount => amount.Attribute(CurrencyIdAttribute)?.Value == currency.Value)
                .ToList();

            return (vat.Count == 1 && AreEqual(withVat, RoundToCents(Add(withoutVat, Decimal(vat[0])))))
                || AreEqual(withVat, withoutVat);
        });
    }

    /// <summary>Whether a UBL amount due adds up, compared as the alternative for the amounts its totals carry asks.</summary>
    private static bool UblAmountDueAddsUp(XElement totals)
    {
        var due = Decimal(totals.Element(Cbc + "PayableAmount"));
        var withVat = Decimal(totals.Element(Cbc + "TaxInclusiveAmount"));
        var paid = Decimal(totals.Element(Cbc + "PrepaidAmount"));
        var rounding = Decimal(totals.Element(Cbc + "PayableRoundingAmount"));
        var hasPaid = totals.Element(Cbc + "PrepaidAmount") is not null;
        var hasRounding = totals.Element(Cbc + "PayableRoundingAmount") is not null;

        return (hasPaid && !hasRounding && AreEqual(due, RoundToCents(Subtract(withVat, paid))))
            || (!hasPaid && !hasRounding && AreEqual(due, withVat))
            || (hasPaid && hasRounding && AreEqual(RoundToCents(Subtract(due, rounding)), RoundToCents(Subtract(withVat, paid))))
            || (!hasPaid && hasRounding && AreEqual(RoundToCents(Subtract(due, rounding)), withVat));
    }

    /// <summary>Whether a CII amount due adds up, in one of the alternatives its schematron allows.</summary>
    private static bool CiiAmountDueAddsUp(XElement totals)
    {
        var due = Decimal(totals.Element(Ram + "DuePayableAmount"));
        var withVat = Decimal(totals.Element(Ram + "GrandTotalAmount"));
        var paid = Decimal(totals.Element(Ram + "TotalPrepaidAmount"));
        var rounding = Decimal(totals.Element(Ram + "RoundingAmount"));
        var hasPaid = totals.Element(Ram + "TotalPrepaidAmount") is not null;
        var hasRounding = totals.Element(Ram + "RoundingAmount") is not null;

        return AreEqual(due, Add(Subtract(withVat, paid), rounding))
            || (AreEqual(due, Add(withVat, rounding)) && !hasPaid)
            || (AreEqual(due, Subtract(withVat, paid)) && !hasRounding)
            || (AreEqual(due, withVat) && !hasPaid && !hasRounding);
    }

    /// <summary>The invoice currency codes (BT-5) of the CII header, as text.</summary>
    private static HashSet<string> InvoiceCurrencies(XElement? settlement) =>
        (settlement?.Elements(Ram + "InvoiceCurrencyCode") ?? []).Select(code => code.Value).ToHashSet(StringComparer.Ordinal);
}
