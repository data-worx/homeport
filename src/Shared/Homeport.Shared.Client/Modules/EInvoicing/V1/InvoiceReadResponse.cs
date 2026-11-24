// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Modules.EInvoicing.V1;

/// <summary>What the e-invoicing module found when it read a document, and the invoice it read.</summary>
/// <param name="Report">What the module found, as it reports it when it validates the document.</param>
/// <param name="Invoice">
/// The invoice in the semantic model of EN 16931, read as far as the document
/// carries it, also when it is invalid; <see langword="null"/> for a ZUGFeRD
/// 1.0 invoice, which is written in no syntax EN 16931 binds to.
/// </param>
public sealed record InvoiceReadResponse(ValidationReportResponse Report, Invoice? Invoice);
