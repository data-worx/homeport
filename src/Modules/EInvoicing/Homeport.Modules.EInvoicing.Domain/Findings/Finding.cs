// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Findings;

/// <summary>Something the module found about a document.</summary>
/// <param name="Severity">How much it weighs.</param>
/// <param name="Rule">The identifier of the rule that found it.</param>
/// <param name="Message">An English description of what was found.</param>
public sealed record Finding(FindingSeverity Severity, string Rule, string Message);
