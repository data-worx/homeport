// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Modules.EInvoicing.Domain.Documents;

/// <summary>The XML syntaxes EN 16931 binds the semantic model of an invoice to.</summary>
public enum InvoiceSyntax
{
    /// <summary>An invoice in OASIS UBL 2.1.</summary>
    UblInvoice,

    /// <summary>A credit note in OASIS UBL 2.1.</summary>
    UblCreditNote,

    /// <summary>A cross industry invoice in UN/CEFACT CII D16B.</summary>
    Cii,
}
