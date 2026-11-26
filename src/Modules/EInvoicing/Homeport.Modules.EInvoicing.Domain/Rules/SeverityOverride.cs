// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.RuleSets;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>How much a finding of a rule weighs under one rule set, where that rule set's validator configuration weighs it otherwise than the rule's source.</summary>
/// <param name="RuleSet">The rule set.</param>
/// <param name="Syntax">The syntax it applies to; none for every syntax.</param>
/// <param name="Severity">How much the finding weighs there.</param>
/// <param name="Reason">Why the rule set weighs it so, in English, as its configuration states.</param>
public sealed record SeverityOverride(RuleSet RuleSet, InvoiceSyntax? Syntax, FindingSeverity Severity, string Reason)
{
    /// <summary>Whether the override applies to an invoice in <paramref name="syntax"/> checked under <paramref name="ruleSet"/>.</summary>
    /// <param name="syntax">The syntax of the invoice.</param>
    /// <param name="ruleSet">The rule set the invoice is checked under.</param>
    public bool AppliesTo(InvoiceSyntax syntax, RuleSet ruleSet) => RuleSet == ruleSet && (Syntax is null || Syntax == syntax);
}
