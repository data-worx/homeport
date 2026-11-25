// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Findings;

/// <summary>Something the module found about a document.</summary>
/// <param name="Severity">How much it weighs.</param>
/// <param name="Rule">The identifier of the rule that found it.</param>
/// <param name="Message">An English description of what was found.</param>
/// <param name="Location">
/// Where in the document it was found, as an XPath with the prefixes the
/// document uses; none when it concerns the document as a whole, or for a
/// business rule, which names the place in the semantic model instead.
/// </param>
/// <param name="Path">Where in the semantic model it was found, as a JSON path; none when the rule does not know.</param>
/// <param name="BusinessTerm">The business term of EN 16931 it concerns, such as <c>BT-1</c>; none when the rule does not know.</param>
public sealed record Finding(
    FindingSeverity Severity,
    string Rule,
    string Message,
    string? Location = null,
    string? Path = null,
    string? BusinessTerm = null);
