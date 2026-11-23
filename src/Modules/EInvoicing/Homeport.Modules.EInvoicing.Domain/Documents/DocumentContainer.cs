// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Documents;

/// <summary>What a document arrives as: an invoice in XML, or a PDF that may carry one.</summary>
public enum DocumentContainer
{
    /// <summary>An XML document, the invoice itself.</summary>
    Xml,

    /// <summary>A PDF, which may carry an invoice as an embedded file.</summary>
    Pdf,
}
