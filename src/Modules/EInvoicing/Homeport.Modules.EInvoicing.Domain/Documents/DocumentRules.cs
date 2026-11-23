// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Contracts;

namespace Homeport.Modules.EInvoicing.Domain.Documents;

/// <summary>The rules that tell which syntax a document is written in.</summary>
public static class DocumentRules
{
    /// <summary>The document cannot be read as what it is declared to be: well-formed XML or a PDF.</summary>
    public const string UnreadableCode = "document.unreadable";

    /// <summary>The document is well-formed XML, but its root element names no invoice syntax.</summary>
    public const string UnknownSyntaxCode = "document.unknown-syntax";

    private const string UblInvoiceNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private const string UblInvoiceElement = "Invoice";
    private const string UblCreditNoteNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";
    private const string UblCreditNoteElement = "CreditNote";
    private const string CiiNamespace = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
    private const string CiiElement = "CrossIndustryInvoice";

    /// <summary>The error for a document that cannot be read as what it is declared to be.</summary>
    /// <param name="reason">An English description of what is wrong with it.</param>
    public static Error Unreadable(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new Error(UnreadableCode, reason);
    }

    /// <summary>The syntax an XML document's root element names.</summary>
    /// <remarks>
    /// The namespace and the local name decide together, compared exactly:
    /// a UBL root element in the wrong namespace is no UBL document.
    /// </remarks>
    /// <param name="namespaceUri">The namespace of the root element, empty for none.</param>
    /// <param name="localName">The local name of the root element.</param>
    public static Result<InvoiceSyntax> SyntaxOf(string namespaceUri, string localName)
    {
        ArgumentNullException.ThrowIfNull(namespaceUri);
        ArgumentNullException.ThrowIfNull(localName);

        return (namespaceUri, localName) switch
        {
            (UblInvoiceNamespace, UblInvoiceElement) => Result<InvoiceSyntax>.Success(InvoiceSyntax.UblInvoice),
            (UblCreditNoteNamespace, UblCreditNoteElement) => Result<InvoiceSyntax>.Success(InvoiceSyntax.UblCreditNote),
            (CiiNamespace, CiiElement) => Result<InvoiceSyntax>.Success(InvoiceSyntax.Cii),
            _ => Result<InvoiceSyntax>.Failure(new Error(
                UnknownSyntaxCode,
                $"The root element '{localName}' in the namespace '{namespaceUri}' is neither a UBL Invoice, a UBL CreditNote nor a CII CrossIndustryInvoice.")),
        };
    }
}
