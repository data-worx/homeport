// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;
using static Homeport.Modules.EInvoicing.Domain.Rules.XPathNumbers;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The decimal rules of EN 16931 (BR-DEC-nn) as the EN 16931 validation
/// artefacts test them: an amount carries at most two decimals. One method
/// per rule returns the JSON path of every place the invoice breaks it.
/// </summary>
/// <remarks>
/// The schematron counts the characters after the first point of the text
/// as written, so <c>1.000</c> and <c>1.00 </c> with a trailing space break
/// a rule. Only the total VAT amounts of CII are compared by value; in UBL
/// the schematron's test of them never fails. The rules are checked on the
/// elements <see cref="RuleContexts"/> names; where CII may repeat an
/// amount, the first counts.
/// </remarks>
public static class DecimalRules
{
    // The number of decimals an amount may carry.
    private const int MaximumDecimals = 2;

    /// <summary>Every decimal rule, by its identifier.</summary>
    public static IReadOnlyDictionary<string, Func<RuleInput, IEnumerable<string>>> All { get; } =
        new Dictionary<string, Func<RuleInput, IEnumerable<string>>>(StringComparer.Ordinal)
        {
            ["BR-DEC-01"] = BrDec01,
            ["BR-DEC-02"] = BrDec02,
            ["BR-DEC-05"] = BrDec05,
            ["BR-DEC-06"] = BrDec06,
            ["BR-DEC-09"] = BrDec09,
            ["BR-DEC-10"] = BrDec10,
            ["BR-DEC-11"] = BrDec11,
            ["BR-DEC-12"] = BrDec12,
            ["BR-DEC-13"] = BrDec13,
            ["BR-DEC-14"] = BrDec14,
            ["BR-DEC-15"] = BrDec15,
            ["BR-DEC-16"] = BrDec16,
            ["BR-DEC-17"] = BrDec17,
            ["BR-DEC-18"] = BrDec18,
            ["BR-DEC-19"] = BrDec19,
            ["BR-DEC-20"] = BrDec20,
            ["BR-DEC-23"] = BrDec23,
            ["BR-DEC-24"] = BrDec24,
            ["BR-DEC-25"] = BrDec25,
            ["BR-DEC-27"] = BrDec27,
            ["BR-DEC-28"] = BrDec28,
        };

    /// <summary>BR-DEC-01: the document level allowance amount (BT-92) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec01(RuleInput input) => TooPrecise(input, RuleContexts.DocumentLevelAllowancesOrCharges(input, isCharge: false), "ActualAmount", "Amount", "amount");

    /// <summary>BR-DEC-02: the document level allowance base amount (BT-93) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec02(RuleInput input) => TooPrecise(input, RuleContexts.DocumentLevelAllowancesOrCharges(input, isCharge: false), "BasisAmount", "BaseAmount", "baseAmount");

    /// <summary>BR-DEC-05: the document level charge amount (BT-99) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec05(RuleInput input) => TooPrecise(input, RuleContexts.DocumentLevelAllowancesOrCharges(input, isCharge: true), "ActualAmount", "Amount", "amount");

    /// <summary>BR-DEC-06: the document level charge base amount (BT-100) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec06(RuleInput input) => TooPrecise(input, RuleContexts.DocumentLevelAllowancesOrCharges(input, isCharge: true), "BasisAmount", "BaseAmount", "baseAmount");

    /// <summary>BR-DEC-09: the sum of invoice line net amounts (BT-106) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec09(RuleInput input) => TooPrecise(input, RuleContexts.DocumentTotals(input), "LineTotalAmount", "LineExtensionAmount", "sumOfLineNetAmounts");

    /// <summary>BR-DEC-10: the sum of allowances on document level (BT-107) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec10(RuleInput input) => TooPrecise(input, RuleContexts.DocumentTotals(input), "AllowanceTotalAmount", "AllowanceTotalAmount", "sumOfAllowances");

    /// <summary>BR-DEC-11: the sum of charges on document level (BT-108) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec11(RuleInput input) => TooPrecise(input, RuleContexts.DocumentTotals(input), "ChargeTotalAmount", "ChargeTotalAmount", "sumOfCharges");

    /// <summary>BR-DEC-12: the invoice total amount without VAT (BT-109) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec12(RuleInput input) => TooPrecise(input, RuleContexts.DocumentTotals(input), "TaxBasisTotalAmount", "TaxExclusiveAmount", "totalWithoutVat");

    /// <summary>BR-DEC-13: the invoice total VAT amount (BT-110) carries at most two decimals.</summary>
    /// <remarks>
    /// CII compares the value of each total VAT amount in the invoice
    /// currency with its value rounded to two decimals, and the rule holds
    /// while any total VAT amount passes or is in another currency. UBL's
    /// test compares the currency with a code below the tax amount, which
    /// never exists, so it never fails.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec13(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var currencies = CurrencyCodes(input, "InvoiceCurrencyCode");

        return CiiTotalVatAmountsTooPrecise(input, amount => !IsOneOfCurrencies(amount, currencies), currencies, "totalVatAmount");
    }

    /// <summary>BR-DEC-14: the invoice total amount with VAT (BT-112) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec14(RuleInput input) => TooPrecise(input, RuleContexts.DocumentTotals(input), "GrandTotalAmount", "TaxInclusiveAmount", "totalWithVat");

    /// <summary>BR-DEC-15: the invoice total VAT amount in accounting currency (BT-111) carries at most two decimals.</summary>
    /// <remarks>
    /// CII compares the value of each total VAT amount in the VAT accounting
    /// currency with its value rounded to two decimals, and the rule holds
    /// while any passes or the invoice names no accounting currency. UBL's
    /// test never fails, as for BR-DEC-13.
    /// </remarks>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec15(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var currencies = CurrencyCodes(input, "TaxCurrencyCode");

        return CiiTotalVatAmountsTooPrecise(input, _ => currencies.Count == 0, currencies, "totalVatAmountInAccountingCurrency");
    }

    /// <summary>BR-DEC-16: the paid amount (BT-113) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec16(RuleInput input) => TooPrecise(input, RuleContexts.DocumentTotals(input), "TotalPrepaidAmount", "PrepaidAmount", "paidAmount");

    /// <summary>BR-DEC-17: the rounding amount (BT-114) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec17(RuleInput input) => TooPrecise(input, RuleContexts.DocumentTotals(input), "RoundingAmount", "PayableRoundingAmount", "roundingAmount");

    /// <summary>BR-DEC-18: the amount due for payment (BT-115) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec18(RuleInput input) => TooPrecise(input, RuleContexts.DocumentTotals(input), "DuePayableAmount", "PayableAmount", "amountDueForPayment");

    /// <summary>BR-DEC-19: the VAT category taxable amount (BT-116) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec19(RuleInput input) => TooPrecise(input, RuleContexts.VatBreakdowns(input), "BasisAmount", "TaxableAmount", "taxableAmount");

    /// <summary>BR-DEC-20: the VAT category tax amount (BT-117) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec20(RuleInput input) => TooPrecise(input, RuleContexts.VatBreakdowns(input), "CalculatedAmount", "TaxAmount", "taxAmount");

    /// <summary>BR-DEC-23: the invoice line net amount (BT-131) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec23(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return input.Lines
            .Select((line, index) => (line, index))
            .Where(entry => FractionLength(input.IsCii
                ? entry.line.Elements(Ram + "SpecifiedLineTradeSettlement").Elements(Ram + "SpecifiedTradeSettlementLineMonetarySummation").Elements(Ram + "LineTotalAmount").FirstOrDefault()
                : entry.line.Element(Cbc + "LineExtensionAmount")) > MaximumDecimals)
            .Select(entry => RulePaths.Line(entry.index) + ".netAmount");
    }

    /// <summary>BR-DEC-24: the invoice line allowance amount (BT-136) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec24(RuleInput input) => TooPrecise(input, RuleContexts.LineLevelAllowancesOrCharges(input, isCharge: false), "ActualAmount", "Amount", "amount");

    /// <summary>BR-DEC-25: the invoice line allowance base amount (BT-137) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec25(RuleInput input) => TooPrecise(input, RuleContexts.LineLevelAllowancesOrCharges(input, isCharge: false), "BasisAmount", "BaseAmount", "baseAmount");

    /// <summary>BR-DEC-27: the invoice line charge amount (BT-141) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec27(RuleInput input) => TooPrecise(input, RuleContexts.LineLevelAllowancesOrCharges(input, isCharge: true), "ActualAmount", "Amount", "amount");

    /// <summary>BR-DEC-28: the invoice line charge base amount (BT-142) carries at most two decimals.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrDec28(RuleInput input) => TooPrecise(input, RuleContexts.LineLevelAllowancesOrCharges(input, isCharge: true), "BasisAmount", "BaseAmount", "baseAmount");

    /// <summary>The paths of the contexts whose amount, named <paramref name="ciiName"/> in CII and <paramref name="ublName"/> in UBL, carries more than two decimals.</summary>
    private static IEnumerable<string> TooPrecise(RuleInput input, IEnumerable<RuleContext> contexts, string ciiName, string ublName, string member) =>
        contexts
            .Where(context => FractionLength(context.Element.Element(input.IsCii ? Ram + ciiName : Cbc + ublName)) > MaximumDecimals)
            .Select(context => context.PathTo(member));

    /// <summary>The paths of the CII document totals none of whose total VAT amounts passes: in one of <paramref name="currencies"/> with at most two decimals, or exempt by <paramref name="exempt"/>.</summary>
    private static IEnumerable<string> CiiTotalVatAmountsTooPrecise(RuleInput input, Func<XElement, bool> exempt, IReadOnlySet<string> currencies, string member)
    {
        if (!input.IsCii)
        {
            return [];
        }

        return RuleContexts.DocumentTotals(input)
            .Where(context => context.Element.Elements(Ram + "TaxTotalAmount").ToList() is { Count: > 0 } amounts
                && !amounts.Any(amount => (IsOneOfCurrencies(amount, currencies) && AreEqual(Decimal(amount), RoundToCents(Decimal(amount)))) || exempt(amount)))
            .Select(context => context.PathTo(member));
    }

    /// <summary>The currency codes the CII header carries in the elements named <paramref name="name"/>, as text; none for UBL.</summary>
    private static HashSet<string> CurrencyCodes(RuleInput input, string name) =>
        (input.IsCii ? RuleContexts.HeaderSettlement(input)?.Elements(Ram + name) ?? [] : [])
            .Select(code => code.Value)
            .ToHashSet(StringComparer.Ordinal);

    /// <summary>Whether the amount carries a currency among <paramref name="currencies"/>.</summary>
    private static bool IsOneOfCurrencies(XElement amount, IReadOnlySet<string> currencies) =>
        amount.Attribute(CurrencyIdAttribute) is { } currency && currencies.Contains(currency.Value);
}
