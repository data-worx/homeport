// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Model;

namespace Homeport.Modules.EInvoicing.Domain.Reading;

/// <summary>Reads an invoice in either syntax into the semantic model of EN 16931.</summary>
/// <remarks>
/// The model is read as far as the document carries it: a term the document
/// leaves out, or writes in a form the module cannot read, is none. Whether
/// the invoice is valid is left to the rules. Extension elements, such as
/// those of XRechnung Extension and ZUGFeRD EXTENDED, are not read.
/// </remarks>
public static class InvoiceReader
{
    /// <summary>Reads the invoice whose root element is <paramref name="root"/>.</summary>
    /// <param name="root">The root element.</param>
    /// <param name="syntax">The syntax the root element names.</param>
    public static Invoice Read(XElement root, InvoiceSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(root);

        return syntax switch
        {
            InvoiceSyntax.UblInvoice => UblInvoiceReader.Read(root, isCreditNote: false),
            InvoiceSyntax.UblCreditNote => UblInvoiceReader.Read(root, isCreditNote: true),
            InvoiceSyntax.Cii => CiiInvoiceReader.Read(root),
            _ => throw new ArgumentOutOfRangeException(nameof(syntax), syntax, "Every syntax has a reader."),
        };
    }
}
