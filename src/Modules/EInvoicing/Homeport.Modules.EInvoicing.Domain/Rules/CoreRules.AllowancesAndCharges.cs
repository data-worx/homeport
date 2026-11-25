// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Reading;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The core rules on allowances and charges, on document level and on line
/// level. They are checked in the document, where the schematron tells an
/// allowance from a charge: UBL by its indicator as an XML Schema boolean,
/// CII by the exact text <c>false</c> or <c>true</c>.
/// </summary>
public static partial class CoreRules
{
    /// <summary>BR-31: every document level allowance carries an amount (BT-92).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br31(RuleInput input) => DocumentLevel(input, isCharge: false, HasAmount, "amount");

    /// <summary>BR-32: every document level allowance carries a VAT category code (BT-95).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br32(RuleInput input) => DocumentLevel(input, isCharge: false, HasVatCategory, "vatCategoryCode");

    /// <summary>BR-33: every document level allowance carries a reason (BT-97) or a reason code (BT-98).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br33(RuleInput input) => DocumentLevel(input, isCharge: false, HasReason, "reason");

    /// <summary>BR-36: every document level charge carries an amount (BT-99).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br36(RuleInput input) => DocumentLevel(input, isCharge: true, HasAmount, "amount");

    /// <summary>BR-37: every document level charge carries a VAT category code (BT-102).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br37(RuleInput input) => DocumentLevel(input, isCharge: true, HasVatCategory, "vatCategoryCode");

    /// <summary>BR-38: every document level charge carries a reason (BT-104) or a reason code (BT-105).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br38(RuleInput input) => DocumentLevel(input, isCharge: true, HasReason, "reason");

    /// <summary>BR-41: every invoice line allowance carries an amount (BT-136).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br41(RuleInput input) => LineLevel(input, isCharge: false, HasAmount, "amount");

    /// <summary>BR-42: every invoice line allowance carries a reason (BT-139) or a reason code (BT-140).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br42(RuleInput input) => LineLevel(input, isCharge: false, HasReason, "reason");

    /// <summary>BR-43: every invoice line charge carries an amount (BT-141).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br43(RuleInput input) => LineLevel(input, isCharge: true, HasAmount, "amount");

    /// <summary>BR-44: every invoice line charge carries a reason (BT-144) or a reason code (BT-145).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> Br44(RuleInput input) => LineLevel(input, isCharge: true, HasReason, "reason");

    /// <summary>The paths of the document level allowances or charges that lack what <paramref name="has"/> asks for.</summary>
    private static IEnumerable<string> DocumentLevel(RuleInput input, bool isCharge, Func<RuleInput, XElement, bool> has, string member)
    {
        ArgumentNullException.ThrowIfNull(input);

        var parent = input.IsCii
            ? input.Root.Element(Rsm + "SupplyChainTradeTransaction")?.Element(Ram + "ApplicableHeaderTradeSettlement")
            : input.Root;
        var list = isCharge ? "$.documentLevelCharges" : "$.documentLevelAllowances";

        return Broken(input, parent, isCharge, has, list, member);
    }

    /// <summary>The paths of the line level allowances or charges that lack what <paramref name="has"/> asks for.</summary>
    private static IEnumerable<string> LineLevel(RuleInput input, bool isCharge, Func<RuleInput, XElement, bool> has, string member)
    {
        ArgumentNullException.ThrowIfNull(input);

        var list = isCharge ? ".charges" : ".allowances";

        return input.Lines.SelectMany((line, index) => Broken(
            input,
            input.IsCii ? line.Element(Ram + "SpecifiedLineTradeSettlement") : line,
            isCharge,
            has,
            RulePaths.Line(index) + list,
            member));
    }

    /// <summary>The paths of the allowances or charges directly below <paramref name="parent"/> that lack what <paramref name="has"/> asks for.</summary>
    /// <remarks>
    /// The schematron's indicator decides which are checked; the position in
    /// the path counts them as the model does, so it names the entry the
    /// model holds.
    /// </remarks>
    private static IEnumerable<string> Broken(RuleInput input, XElement? parent, bool isCharge, Func<RuleInput, XElement, bool> has, string list, string member)
    {
        var entries = (parent?.Elements(input.IsCii ? Ram + "SpecifiedTradeAllowanceCharge" : Cac + "AllowanceCharge") ?? []).ToList();
        var modelEntries = entries.Where(entry => IsModelCharge(input, entry) == isCharge).ToList();

        return entries
            .Where(entry => IsRuleCharge(input, entry, isCharge) && !has(input, entry))
            .Select(entry => IndexOf(modelEntries, entry) is var index and >= 0 ? $"{RulePaths.Item(list, index)}.{member}" : list);
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

    /// <summary>Whether an allowance or charge carries an amount.</summary>
    private static bool HasAmount(RuleInput input, XElement entry) =>
        entry.Element(input.IsCii ? Ram + "ActualAmount" : Cbc + "Amount") is not null;

    /// <summary>Whether an allowance or charge carries a VAT category code: in a tax category of VAT, or a trade tax of the type VAT.</summary>
    private static bool HasVatCategory(RuleInput input, XElement entry) => input.IsCii
        ? entry.Elements(Ram + "CategoryTradeTax").Where(IsCiiVat).Elements(Ram + "CategoryCode").Any()
        : entry.Elements(Cac + "TaxCategory").Where(IsUblVat).Elements(Cbc + "ID").Any();

    /// <summary>Whether an allowance or charge carries a reason or a reason code.</summary>
    private static bool HasReason(RuleInput input, XElement entry) => input.IsCii
        ? entry.Element(Ram + "Reason") is not null || entry.Element(Ram + "ReasonCode") is not null
        : entry.Element(Cbc + "AllowanceChargeReason") is not null || entry.Element(Cbc + "AllowanceChargeReasonCode") is not null;
}
