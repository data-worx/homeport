// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.RuleSets;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>A business rule as the rule catalog lists it.</summary>
/// <param name="Id">The official identifier of the rule, such as <c>BR-01</c>.</param>
/// <param name="Group">The group the rule belongs to, such as <c>EN 16931 core rules</c>.</param>
/// <param name="Standard">The name of the standard in the module's <c>standards.json</c> the rule is taken from.</param>
/// <param name="BusinessTerm">The business term or group of EN 16931 the rule concerns; none when it concerns none.</param>
/// <param name="Severity">How much a finding of the rule weighs.</param>
/// <param name="SeverityInUbl">How much it weighs in UBL, where the source weighs it otherwise; none where it does not.</param>
/// <param name="SeverityInCii">How much it weighs in CII, where the source weighs it otherwise; none where it does not.</param>
/// <param name="Message">The English description of what a finding of the rule means.</param>
/// <param name="RuleSets">The rule sets whose invoices are checked against the rule, in every version.</param>
public sealed record RuleDefinition(
    string Id,
    string Group,
    string Standard,
    string? BusinessTerm,
    FindingSeverity Severity,
    FindingSeverity? SeverityInUbl,
    FindingSeverity? SeverityInCii,
    string Message,
    IReadOnlySet<RuleSet> RuleSets)
{
    /// <summary>How much the rule weighs under a rule set whose validator configuration weighs it otherwise; none where none does.</summary>
    public IReadOnlyList<SeverityOverride> Overrides { get; init; } = [];

    /// <summary>How much a finding of the rule weighs in an invoice written in <paramref name="syntax"/> and checked under <paramref name="ruleSet"/>.</summary>
    /// <remarks>An override of the rule set wins over the severity of the syntax, which wins over the rule's own.</remarks>
    /// <param name="syntax">The syntax of the invoice.</param>
    /// <param name="ruleSet">The rule set the invoice is checked under.</param>
    public FindingSeverity SeverityIn(InvoiceSyntax syntax, RuleSet ruleSet) =>
        Overrides.FirstOrDefault(entry => entry.AppliesTo(syntax, ruleSet))?.Severity
        ?? (syntax == InvoiceSyntax.Cii ? SeverityInCii : SeverityInUbl)
        ?? Severity;
}
