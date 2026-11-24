// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>Something the module found about a document.</summary>
/// <param name="Severity">How much it weighs.</param>
/// <param name="Rule">The identifier of the rule that found it, such as <c>EINV-SCHEMA</c>.</param>
/// <param name="Message">An English description of what was found.</param>
/// <param name="Location">
/// Where in the document it was found, as an XPath with the prefixes the
/// document uses, such as <c>/Invoice/cac:InvoiceLine[2]/cbc:InvoicedQuantity[1]</c>;
/// <see langword="null"/> when it concerns the document as a whole.
/// </param>
/// <param name="Path">
/// Where in the invoice of <see cref="InvoiceReadResponse"/> it was found, as
/// a JSON path; <see langword="null"/> when the rule does not know.
/// </param>
/// <param name="BusinessTerm">
/// The business term of EN 16931 it concerns, such as <c>BT-1</c>;
/// <see langword="null"/> when the rule does not know.
/// </param>
public sealed record ValidationFinding(
    FindingSeverity Severity,
    string Rule,
    string Message,
    string? Location,
    string? Path,
    string? BusinessTerm);
