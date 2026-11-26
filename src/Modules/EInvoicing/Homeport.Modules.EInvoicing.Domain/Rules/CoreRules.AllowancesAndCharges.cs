// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using static Homeport.Modules.EInvoicing.Domain.Rules.RuleXml;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The core rules on allowances and charges, on document level and on line
/// level. They are checked in the document, on the allowances and charges
/// <see cref="RuleContexts"/> names.
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
    private static IEnumerable<string> DocumentLevel(RuleInput input, bool isCharge, Func<RuleInput, XElement, bool> has, string member) =>
        RuleContexts.DocumentLevelAllowancesOrCharges(input, isCharge).Where(context => !has(input, context.Element)).Select(context => context.PathTo(member));

    /// <summary>The paths of the line level allowances or charges that lack what <paramref name="has"/> asks for.</summary>
    private static IEnumerable<string> LineLevel(RuleInput input, bool isCharge, Func<RuleInput, XElement, bool> has, string member) =>
        RuleContexts.LineLevelAllowancesOrCharges(input, isCharge).Where(context => !has(input, context.Element)).Select(context => context.PathTo(member));

    /// <summary>Whether an allowance or charge carries an amount.</summary>
    private static bool HasAmount(RuleInput input, XElement entry) =>
        entry.Element(input.IsCii ? Ram + "ActualAmount" : Cbc + "Amount") is not null;

    /// <summary>Whether an allowance or charge carries a VAT category code: in a tax category of VAT, or a trade tax of the type VAT.</summary>
    private static bool HasVatCategory(RuleInput input, XElement entry) => input.IsCii
        ? entry.Elements(Ram + "CategoryTradeTax").Where(IsCiiVat).Elements(Ram + "CategoryCode").Any()
        : entry.Elements(Cac + "TaxCategory").Where(IsUblVat).Elements(Cbc + "ID").Any();

    /// <summary>Whether an allowance or charge carries a reason or a reason code.</summary>
    /// <param name="input">The invoice.</param>
    /// <param name="entry">The allowance or charge.</param>
    internal static bool HasReason(RuleInput input, XElement entry) => input.IsCii
        ? entry.Element(Ram + "Reason") is not null || entry.Element(Ram + "ReasonCode") is not null
        : entry.Element(Cbc + "AllowanceChargeReason") is not null || entry.Element(Cbc + "AllowanceChargeReasonCode") is not null;
}
