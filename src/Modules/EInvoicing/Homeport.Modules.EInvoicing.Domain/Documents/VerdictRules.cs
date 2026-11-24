// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Modules.EInvoicing.Domain.Findings;

namespace Homeport.Modules.EInvoicing.Domain.Documents;

/// <summary>The rule that turns the findings on an e-invoice into a verdict.</summary>
public static class VerdictRules
{
    /// <summary><see cref="Verdict.Invalid"/> with at least one fatal finding, <see cref="Verdict.Valid"/> without.</summary>
    /// <param name="findings">Everything found about the e-invoice.</param>
    public static Verdict VerdictOf(IEnumerable<Finding> findings)
    {
        ArgumentNullException.ThrowIfNull(findings);

        return findings.Any(finding => finding.Severity == FindingSeverity.Fatal) ? Verdict.Invalid : Verdict.Valid;
    }
}
