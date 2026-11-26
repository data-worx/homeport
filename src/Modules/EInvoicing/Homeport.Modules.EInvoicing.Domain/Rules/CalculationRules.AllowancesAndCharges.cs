// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The calculation rules on allowances and charges, on the entries <see cref="RuleContexts"/> names.</summary>
/// <remarks>
/// BR-CO-05 to BR-CO-08 ask that a reason code and a reason describe the same
/// allowance or charge. No machine can tell, so the schematron's test always
/// holds, and so does the module's: it never reports them.
/// </remarks>
public static partial class CalculationRules
{
    /// <summary>BR-CO-05: the reason code (BT-98) and the reason (BT-97) of a document level allowance describe the same allowance; not machine-checkable, never broken.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo05(RuleInput input) => NotMachineCheckable(input);

    /// <summary>BR-CO-06: the reason code (BT-105) and the reason (BT-104) of a document level charge describe the same charge; not machine-checkable, never broken.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo06(RuleInput input) => NotMachineCheckable(input);

    /// <summary>BR-CO-07: the reason code (BT-140) and the reason (BT-139) of an invoice line allowance describe the same allowance; not machine-checkable, never broken.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo07(RuleInput input) => NotMachineCheckable(input);

    /// <summary>BR-CO-08: the reason code (BT-145) and the reason (BT-144) of an invoice line charge describe the same charge; not machine-checkable, never broken.</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo08(RuleInput input) => NotMachineCheckable(input);

    /// <summary>BR-CO-21: every document level allowance carries a reason (BT-97) or a reason code (BT-98).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo21(RuleInput input) => WithoutReason(RuleContexts.DocumentLevelAllowancesOrCharges(input, isCharge: false), input);

    /// <summary>BR-CO-22: every document level charge carries a reason (BT-104) or a reason code (BT-105).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo22(RuleInput input) => WithoutReason(RuleContexts.DocumentLevelAllowancesOrCharges(input, isCharge: true), input);

    /// <summary>BR-CO-23: every invoice line allowance carries a reason (BT-139) or a reason code (BT-140).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo23(RuleInput input) => WithoutReason(RuleContexts.LineLevelAllowancesOrCharges(input, isCharge: false), input);

    /// <summary>BR-CO-24: every invoice line charge carries a reason (BT-144) or a reason code (BT-145).</summary>
    /// <param name="input">The invoice.</param>
    public static IEnumerable<string> BrCo24(RuleInput input) => WithoutReason(RuleContexts.LineLevelAllowancesOrCharges(input, isCharge: true), input);

    /// <summary>Nothing: a rule no machine can check holds for every invoice.</summary>
    private static string[] NotMachineCheckable(RuleInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        return [];
    }

    /// <summary>The paths of the allowances or charges that carry neither a reason nor a reason code.</summary>
    private static IEnumerable<string> WithoutReason(IEnumerable<RuleContext> contexts, RuleInput input) =>
        contexts.Where(context => !CoreRules.HasReason(input, context.Element)).Select(context => context.PathTo("reason"));
}
