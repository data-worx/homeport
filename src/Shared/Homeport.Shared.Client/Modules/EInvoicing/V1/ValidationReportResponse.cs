// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>What the e-invoicing module found when it validated a document.</summary>
/// <param name="Container">What the document arrived as.</param>
/// <param name="Syntax">
/// The syntax of the invoice; <see langword="null"/> for a PDF, whose embedded
/// invoice is not read yet.
/// </param>
public sealed record ValidationReportResponse(DocumentContainer Container, InvoiceSyntax? Syntax);
