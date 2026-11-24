// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>What the e-invoicing module found when it validated a document.</summary>
/// <param name="Container">What the document arrived as.</param>
/// <param name="Syntax">
/// The syntax of the invoice; <see langword="null"/> for a ZUGFeRD 1.0
/// invoice, which is written in none EN 16931 binds to.
/// </param>
/// <param name="CustomizationId">
/// The specification identifier (BT-24) the invoice carries;
/// <see langword="null"/> for a ZUGFeRD 1.0 invoice.
/// </param>
/// <param name="RuleSet">
/// The rule set the invoice is checked against; <see langword="null"/> for a
/// document that is no e-invoice.
/// </param>
/// <param name="RuleSetVersion">
/// The version of <paramref name="RuleSet"/>, as its specification identifier
/// names it, such as <c>3.0</c>; <see langword="null"/> with it.
/// </param>
/// <param name="Verdict">
/// What the module concludes: <see cref="V1.Verdict.NotAnEInvoice"/> for a
/// document that is no e-invoice, otherwise whether the e-invoice is valid.
/// </param>
/// <param name="Findings">What the module found; empty when it found nothing.</param>
public sealed record ValidationReportResponse(
    DocumentContainer Container,
    InvoiceSyntax? Syntax,
    string? CustomizationId,
    RuleSet? RuleSet,
    string? RuleSetVersion,
    Verdict Verdict,
    IReadOnlyList<ValidationFinding> Findings);
