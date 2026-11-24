// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml;
using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Modules.EInvoicing.Domain.Findings;
using Homeport.Modules.EInvoicing.Domain.Model;
using Homeport.Modules.EInvoicing.Domain.Reading;
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
        var loaded = await LoadAsync(document, container, cancellationToken);

        return loaded.IsSuccess
            ? Result<InspectedDocument>.Success(loaded.Value.Document)
            : Result<InspectedDocument>.Failure(loaded.Error);
    }

    /// <summary>
    /// What <paramref name="document"/> is and claims and the invoice it holds
    /// in the semantic model of EN 16931, or why it cannot be read as
    /// <paramref name="container"/>.
    /// </summary>
    /// <remarks>
    /// The document is inspected as <see cref="InspectAsync"/> inspects it.
    /// The invoice is read as far as the document carries it; a ZUGFeRD 1.0
    /// invoice, written in no syntax of EN 16931, yields none. An invoice that
    /// claims a rule set extending EN 16931 gets a warning that its extension
    /// was not read.
    /// </remarks>
    /// <param name="document">The document as it was sent.</param>
    /// <param name="container">What the caller declares the document to be.</param>
    /// <param name="cancellationToken">Cancels reading the document.</param>
    public async Task<Result<ReadDocument>> ReadAsync(Stream document, DocumentContainer container, CancellationToken cancellationToken)
    {
        var loaded = await LoadAsync(document, container, cancellationToken);
        if (!loaded.IsSuccess)
        {
            return Result<ReadDocument>.Failure(loaded.Error);
        }

        var inspected = loaded.Value.Document;
        if (inspected.Syntax is not { } syntax)
        {
            return Result<ReadDocument>.Success(new ReadDocument(inspected, Invoice: null));
        }

        var invoice = InvoiceReader.Read(loaded.Value.Root, syntax);
        if (inspected.Match is { } match && RuleSetRules.CheckExtension(match) is { } ignored)
        {
            inspected = inspected with { Findings = [.. inspected.Findings, ignored] };
        }

        return Result<ReadDocument>.Success(new ReadDocument(inspected, invoice));
    }

    /// <summary>Inspects a document and keeps the root element of the invoice it holds.</summary>
    private async Task<Result<LoadedDocument>> LoadAsync(Stream document, DocumentContainer container, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        return container == DocumentContainer.Pdf
            ? await InspectPdfAsync(document, cancellationToken)
            : await InspectXmlAsync(document, DocumentContainer.Xml, cancellationToken);
    }

    /// <summary>Takes the invoice out of a PDF and inspects it; compares the profile the PDF declares.</summary>
    private async Task<Result<LoadedDocument>> InspectPdfAsync(Stream document, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await document.CopyToAsync(buffer, cancellationToken);

        var extracted = PdfInvoiceExtraction.Extract(buffer.ToArray());
        if (!extracted.IsSuccess)
        {
            return Result<LoadedDocument>.Failure(extracted.Error);
        }

        using var invoice = new MemoryStream(extracted.Value.Invoice, writable: false);
        var inspected = await InspectXmlAsync(invoice, DocumentContainer.Pdf, cancellationToken);
        if (!inspected.IsSuccess
            || extracted.Value.DeclaredProfile is not { } declared
            || inspected.Value.Document.Match is not { } match
            || RuleSetRules.CheckProfile(match, declared) is not { } mismatch)
        {
            return inspected;
        }

        var found = inspected.Value.Document;

        return Result<LoadedDocument>.Success(inspected.Value with { Document = found with { Findings = [.. found.Findings, mismatch] } });
    }

    /// <summary>Reads an XML document to its end and tells its syntax and the rule set it claims.</summary>
    private async Task<Result<LoadedDocument>> InspectXmlAsync(Stream document, DocumentContainer container, CancellationToken cancellationToken)
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

            return Result<LoadedDocument>.Failure(
                DocumentRules.Unreadable($"{what} is not well-formed XML without a DTD: {exception.Message}"));
        }

        var namespaceUri = root.Name.NamespaceName;
        var localName = root.Name.LocalName;
        if (DocumentRules.IsZugferd1(namespaceUri, localName))
        {
            return Result<LoadedDocument>.Success(new LoadedDocument(
                new InspectedDocument(container, Syntax: null, Match: null, Verdict.NotAnEInvoice, Findings: []),
                root));
        }

        var syntax = DocumentRules.SyntaxOf(namespaceUri, localName);
        if (!syntax.IsSuccess)
        {
            return Result<LoadedDocument>.Failure(syntax.Error);
        }

        var match = RuleSetRules.Identify(
            catalog,
            DocumentRules.CustomizationIdOf(root, syntax.Value),
            DocumentRules.IssueDateOf(root, syntax.Value));
        if (!match.IsSuccess)
        {
            return Result<LoadedDocument>.Failure(match.Error);
        }

        return Result<LoadedDocument>.Success(new LoadedDocument(
            new InspectedDocument(
                container,
                syntax.Value,
                match.Value,
                match.Value.IsNotAnEInvoice ? Verdict.NotAnEInvoice : null,
                match.Value.Findings),
            root));
    }

    /// <summary>A document as far as it was inspected, with the root element of its invoice.</summary>
    /// <param name="Document">What the inspection found.</param>
    /// <param name="Root">The root element of the invoice.</param>
    private sealed record LoadedDocument(InspectedDocument Document, XElement Root);
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

/// <summary>A document as far as <see cref="DocumentInspection"/> has read it, with the invoice it holds.</summary>
/// <param name="Report">What the inspection found.</param>
/// <param name="Invoice">The invoice in the semantic model of EN 16931; none for a ZUGFeRD 1.0 invoice.</param>
public sealed record ReadDocument(InspectedDocument Report, Invoice? Invoice);
