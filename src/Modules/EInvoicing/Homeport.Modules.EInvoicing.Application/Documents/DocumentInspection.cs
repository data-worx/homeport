// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Shared.Contracts;

namespace Homeport.Modules.EInvoicing.Application.Documents;

/// <summary>Reads a document far enough to tell its container and its syntax.</summary>
public sealed class DocumentInspection
{
    // A DTD is refused rather than ignored: no invoice syntax uses one, and
    // processing it opens the door to entity expansion and external entities.
    // Async, because the body of a request is read asynchronously.
    private static readonly XmlReaderSettings XmlSettings = new()
    {
        Async = true,
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        CloseInput = false,
    };

    /// <summary>Every PDF starts with this header (ISO 32000-2, 7.5.2).</summary>
    private static ReadOnlySpan<byte> PdfHeader => "%PDF-"u8;

    /// <summary>
    /// The container and, for XML, the syntax of <paramref name="document"/>, or
    /// why it cannot be read as <paramref name="container"/>.
    /// </summary>
    /// <remarks>
    /// An XML document is read to its end, so one that is not well-formed is
    /// refused even when its root element names a known syntax. A PDF is only
    /// recognised by its header; the invoice it carries is not read yet.
    /// </remarks>
    /// <param name="document">The document as it was sent.</param>
    /// <param name="container">What the caller declares the document to be.</param>
    /// <param name="cancellationToken">Cancels reading the document.</param>
    public async Task<Result<InspectedDocument>> InspectAsync(Stream document, DocumentContainer container, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        return container == DocumentContainer.Pdf
            ? await InspectPdfAsync(document, cancellationToken)
            : await InspectXmlAsync(document, cancellationToken);
    }

    /// <summary>Recognises a PDF by its header.</summary>
    private static async Task<Result<InspectedDocument>> InspectPdfAsync(Stream document, CancellationToken cancellationToken)
    {
        var header = new byte[PdfHeader.Length];
        var read = await document.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);

        return read == header.Length && PdfHeader.SequenceEqual(header)
            ? Result<InspectedDocument>.Success(new InspectedDocument(DocumentContainer.Pdf, Syntax: null))
            : Result<InspectedDocument>.Failure(DocumentRules.Unreadable("The document does not start with a PDF header."));
    }

    /// <summary>Reads an XML document to its end and tells its syntax from the root element.</summary>
    private static async Task<Result<InspectedDocument>> InspectXmlAsync(Stream document, CancellationToken cancellationToken)
    {
        string namespaceUri;
        string localName;

        try
        {
            using var reader = XmlReader.Create(document, XmlSettings);
            await reader.MoveToContentAsync();
            namespaceUri = reader.NamespaceURI;
            localName = reader.LocalName;

            while (await reader.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        catch (XmlException exception)
        {
            // The parser's way of saying that the document is not well-formed
            // or carries a DTD: an expected outcome, so it becomes a result.
            return Result<InspectedDocument>.Failure(
                DocumentRules.Unreadable($"The document is not well-formed XML without a DTD: {exception.Message}"));
        }

        var syntax = DocumentRules.SyntaxOf(namespaceUri, localName);

        return syntax.IsSuccess
            ? Result<InspectedDocument>.Success(new InspectedDocument(DocumentContainer.Xml, syntax.Value))
            : Result<InspectedDocument>.Failure(syntax.Error);
    }
}

/// <summary>A document as far as <see cref="DocumentInspection"/> has read it.</summary>
/// <param name="Container">What the document arrived as.</param>
/// <param name="Syntax">The syntax of the invoice; none for a PDF until the invoice it carries is read.</param>
public sealed record InspectedDocument(DocumentContainer Container, InvoiceSyntax? Syntax);
