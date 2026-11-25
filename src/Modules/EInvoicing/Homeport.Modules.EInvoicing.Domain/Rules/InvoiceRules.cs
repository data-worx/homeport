// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.RuleSets;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>The second stage of validation: an invoice that fits its XML schema against the business rules of its rule set.</summary>
public static class InvoiceRules
{
    /// <summary>Every rule the module implements, by its identifier: it returns the JSON path of every place it is broken.</summary>
    public static IReadOnlyDictionary<string, Func<RuleInput, IEnumerable<string>>> Implementations { get; } = CoreRules.All;

    /// <summary>The findings of <paramref name="input"/> against every rule of <paramref name="catalog"/> its rule set is checked against.</summary>
    /// <remarks>
    /// Only an invoice that fits its XML schema is checked, as the KoSIT
    /// validator does: the rules assume the structure and the data types the
    /// schema guarantees. A finding carries the rule's message, its severity
    /// in the syntax of the invoice and the JSON path of the place in the
    /// semantic model; the location in the document is left out.
    /// </remarks>
    /// <param name="input">The invoice and the document it was read from.</param>
    /// <param name="ruleSet">The rule set the invoice claims.</param>
    /// <param name="catalog">The rules.</param>
    /// <exception cref="InvalidOperationException">The catalog lists a rule the module does not implement, a defect of the build.</exception>
    public static IReadOnlyList<Finding> Validate(RuleInput input, RuleSet ruleSet, RuleCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(catalog);

        var findings = new List<Finding>();
        foreach (var rule in catalog.Rules.Where(rule => rule.RuleSets.Contains(ruleSet)))
        {
            var check = Implementations.GetValueOrDefault(rule.Id)
                ?? throw new InvalidOperationException($"The rule catalog lists the rule '{rule.Id}', which the module does not implement.");

            findings.AddRange(check(input).Select(path => new Finding(
                rule.SeverityIn(input.Syntax),
                rule.Id,
                rule.Message,
                Location: null,
                Path: path,
                BusinessTerm: rule.BusinessTerm)));
        }

        return findings;
    }
}
