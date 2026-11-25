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
    /// <summary>How much a finding of the rule weighs in an invoice written in <paramref name="syntax"/>.</summary>
    /// <param name="syntax">The syntax of the invoice.</param>
    public FindingSeverity SeverityIn(InvoiceSyntax syntax) =>
        (syntax == InvoiceSyntax.Cii ? SeverityInCii : SeverityInUbl) ?? Severity;
}
