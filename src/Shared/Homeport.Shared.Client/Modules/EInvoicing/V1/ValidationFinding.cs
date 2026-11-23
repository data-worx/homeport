// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>Something the module found about a document.</summary>
/// <param name="Severity">How much it weighs.</param>
/// <param name="Rule">The identifier of the rule that found it, such as <c>EINV-RULE-SET-WINDOW</c>.</param>
/// <param name="Message">An English description of what was found.</param>
public sealed record ValidationFinding(FindingSeverity Severity, string Rule, string Message);
