// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml;
using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.RuleSets;
using Homeport.Shared.Contracts;

namespace Homeport.Modules.EInvoicing.Application.Documents;

/// <summary>Reads a document far enough to tell its container, its syntax and the rule set it claims.</summary>
/// <param name="catalog">The rule sets the module knows.</param>
public sealed class DocumentInspection(RuleSetCatalog catalog)
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

    /// <summary>
    /// What <paramref name="document"/> is and claims, or why it cannot be read
    /// as <paramref name="container"/>.
    /// </summary>
    /// <remarks>
    /// An XML document is read to its end, so one that is not well-formed is
    /// refused even when its root element names a known syntax. A PDF is read
    /// for the invoice it carries, which is then inspected like an XML document.
    /// </remarks>
    /// <param name="document">The document as it was sent.</param>
    /// <param name="container">What the caller declares the document to be.</param>
    /// <param name="cancellationToken">Cancels reading the document.</param>
    public async Task<Result<InspectedDocument>> InspectAsync(Stream document, DocumentContainer container, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        return container == DocumentContainer.Pdf
            ? await InspectPdfAsync(document, cancellationToken)
            : await InspectXmlAsync(document, DocumentContainer.Xml, cancellationToken);
    }

    /// <summary>Takes the invoice out of a PDF and inspects it; compares the profile the PDF declares.</summary>
    private async Task<Result<InspectedDocument>> InspectPdfAsync(Stream document, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await document.CopyToAsync(buffer, cancellationToken);

        var extracted = PdfInvoiceExtraction.Extract(buffer.ToArray());
        if (!extracted.IsSuccess)
        {
            return Result<InspectedDocument>.Failure(extracted.Error);
        }

        using var invoice = new MemoryStream(extracted.Value.Invoice, writable: false);
        var inspected = await InspectXmlAsync(invoice, DocumentContainer.Pdf, cancellationToken);
        if (!inspected.IsSuccess
            || extracted.Value.DeclaredProfile is not { } declared
            || inspected.Value.Match is not { } match
            || RuleSetRules.CheckProfile(match, declared) is not { } mismatch)
        {
            return inspected;
        }

        return Result<InspectedDocument>.Success(inspected.Value with { Findings = [.. inspected.Value.Findings, mismatch] });
    }

    /// <summary>Reads an XML document to its end and tells its syntax and the rule set it claims.</summary>
    private async Task<Result<InspectedDocument>> InspectXmlAsync(Stream document, DocumentContainer container, CancellationToken cancellationToken)
    {
        XElement root;

        try
        {
            using var reader = XmlReader.Create(document, XmlSettings);
            root = (await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken)).Root
                ?? throw new XmlException("The document has no root element.");
        }
        catch (XmlException exception)
        {
            // The parser's way of saying that the document is not well-formed
            // or carries a DTD: an expected outcome, so it becomes a result.
            var what = container == DocumentContainer.Pdf ? "The invoice the PDF carries" : "The document";

            return Result<InspectedDocument>.Failure(
                DocumentRules.Unreadable($"{what} is not well-formed XML without a DTD: {exception.Message}"));
        }

        var namespaceUri = root.Name.NamespaceName;
        var localName = root.Name.LocalName;
        if (DocumentRules.IsZugferd1(namespaceUri, localName))
        {
            return Result<InspectedDocument>.Success(new InspectedDocument(container, Syntax: null, Match: null, Verdict.NotAnEInvoice, Findings: []));
        }

        var syntax = DocumentRules.SyntaxOf(namespaceUri, localName);
        if (!syntax.IsSuccess)
        {
            return Result<InspectedDocument>.Failure(syntax.Error);
        }

        var match = RuleSetRules.Identify(
            catalog,
            DocumentRules.CustomizationIdOf(root, syntax.Value),
            DocumentRules.IssueDateOf(root, syntax.Value));
        if (!match.IsSuccess)
        {
            return Result<InspectedDocument>.Failure(match.Error);
        }

        return Result<InspectedDocument>.Success(new InspectedDocument(
            container,
            syntax.Value,
            match.Value,
            match.Value.IsNotAnEInvoice ? Verdict.NotAnEInvoice : null,
            match.Value.Findings));
    }
}

/// <summary>A document as far as <see cref="DocumentInspection"/> has read it.</summary>
/// <param name="Container">What the document arrived as.</param>
/// <param name="Syntax">The syntax of the invoice; none for a ZUGFeRD 1.0 invoice, which is written in none EN 16931 binds to.</param>
/// <param name="Match">The rule set the invoice claims; none for a ZUGFeRD 1.0 invoice.</param>
/// <param name="Verdict">What the module concludes; none until the rules have run.</param>
/// <param name="Findings">What the inspection found.</param>
public sealed record InspectedDocument(
    DocumentContainer Container,
    InvoiceSyntax? Syntax,
    RuleSetMatch? Match,
    Verdict? Verdict,
    IReadOnlyList<Finding> Findings);
