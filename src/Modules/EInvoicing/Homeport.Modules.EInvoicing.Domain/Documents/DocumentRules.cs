// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Xml.Linq;
using Homeport.Shared.Contracts;

namespace Homeport.Modules.EInvoicing.Domain.Documents;

/// <summary>The rules that tell which syntax a document is written in and what its header claims.</summary>
public static class DocumentRules
{
    /// <summary>The document cannot be read as what it is declared to be: well-formed XML or a PDF.</summary>
    public const string UnreadableCode = "document.unreadable";

    /// <summary>The document is well-formed XML, but its root element names no invoice syntax.</summary>
    public const string UnknownSyntaxCode = "document.unknown-syntax";

    /// <summary>The document is a PDF, but carries no invoice under a file name the module knows.</summary>
    public const string PdfWithoutInvoiceCode = "document.pdf-without-invoice";

    private const string UblInvoiceNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";
    private const string UblInvoiceElement = "Invoice";
    private const string UblCreditNoteNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CreditNote-2";
    private const string UblCreditNoteElement = "CreditNote";
    private const string CiiNamespace = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
    private const string CiiElement = "CrossIndustryInvoice";

    // ZUGFeRD 1.0, as its schema ZUGFeRD1p0.xsd declares it.
    private const string Zugferd1Namespace = "urn:ferd:CrossIndustryDocument:invoice:1p0";
    private const string Zugferd1Element = "CrossIndustryDocument";

    private const string UblBasicNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private const string CiiAggregateNamespace = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";
    private const string CiiUnqualifiedNamespace = "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100";

    // The UBL date format and the CII date format 102 (UNTDID 2379), EN 16931-3.
    private const string UblDateFormat = "yyyy-MM-dd";
    private const string CiiDateFormatCode = "102";
    private const string CiiDateFormat = "yyyyMMdd";

    // The PDF/A extension schemas of Factur-X 1.0 and, deprecated but still
    // supported, of ZUGFeRD 2.0 (Factur-X 1.09.2 / ZUGFeRD 2.5.2, 6.3).
    private const string FacturXXmpNamespace = "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#";
    private const string ZugferdXmpNamespace = "urn:zugferd:pdfa:CrossIndustryDocument:invoice:1p0#";
    private const string ConformanceLevel = "ConformanceLevel";

    private static readonly XName UblCustomizationId = XName.Get("CustomizationID", UblBasicNamespace);
    private static readonly XName UblIssueDate = XName.Get("IssueDate", UblBasicNamespace);
    private static readonly XName CiiContext = XName.Get("ExchangedDocumentContext", CiiNamespace);
    private static readonly XName CiiGuideline = XName.Get("GuidelineSpecifiedDocumentContextParameter", CiiAggregateNamespace);
    private static readonly XName CiiId = XName.Get("ID", CiiAggregateNamespace);
    private static readonly XName CiiDocument = XName.Get("ExchangedDocument", CiiNamespace);
    private static readonly XName CiiIssueDateTime = XName.Get("IssueDateTime", CiiAggregateNamespace);
    private static readonly XName CiiDateTimeString = XName.Get("DateTimeString", CiiUnqualifiedNamespace);
    private static readonly XName CiiDateFormatAttribute = XName.Get("format");

    /// <summary>
    /// The names under which a PDF carries its invoice, in the order they are
    /// looked for: Factur-X and ZUGFeRD 2, XRechnung, ZUGFeRD 2.0 and ZUGFeRD 1.0.
    /// </summary>
    public static IReadOnlyList<string> EmbeddedInvoiceNames { get; } =
        ["factur-x.xml", "xrechnung.xml", "zugferd-invoice.xml", "ZUGFeRD-invoice.xml"];

    /// <summary>The error for a document that cannot be read as what it is declared to be.</summary>
    /// <param name="reason">An English description of what is wrong with it.</param>
    public static Error Unreadable(string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new Error(UnreadableCode, reason);
    }

    /// <summary>The error for a PDF that carries no invoice under a name the module knows.</summary>
    public static Error PdfWithoutInvoice() => new(
        PdfWithoutInvoiceCode,
        $"The PDF carries no embedded file named {string.Join(", ", EmbeddedInvoiceNames.Select(name => $"'{name}'"))}.");

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

    /// <summary>Whether a root element is a ZUGFeRD 1.0 invoice, which predates EN 16931 and is no e-invoice under it.</summary>
    /// <param name="namespaceUri">The namespace of the root element, empty for none.</param>
    /// <param name="localName">The local name of the root element.</param>
    public static bool IsZugferd1(string namespaceUri, string localName) =>
        namespaceUri == Zugferd1Namespace && localName == Zugferd1Element;

    /// <summary>The specification identifier (BT-24) of an invoice, trimmed; none when it carries none.</summary>
    /// <param name="root">The root element of the invoice.</param>
    /// <param name="syntax">The syntax the root element names.</param>
    public static string? CustomizationIdOf(XElement root, InvoiceSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(root);

        var element = syntax == InvoiceSyntax.Cii
            ? root.Element(CiiContext)?.Element(CiiGuideline)?.Element(CiiId)
            : root.Element(UblCustomizationId);
        var value = element?.Value.Trim();

        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>The issue date (BT-2) of an invoice; none when it carries none in the format of its syntax.</summary>
    /// <remarks>
    /// UBL writes it as <c>YYYY-MM-DD</c>, CII as <c>YYYYMMDD</c> with the
    /// format code 102. Any other form is left to the validation rules.
    /// </remarks>
    /// <param name="root">The root element of the invoice.</param>
    /// <param name="syntax">The syntax the root element names.</param>
    public static DateOnly? IssueDateOf(XElement root, InvoiceSyntax syntax)
    {
        ArgumentNullException.ThrowIfNull(root);

        if (syntax != InvoiceSyntax.Cii)
        {
            return ParseDate(root.Element(UblIssueDate)?.Value, UblDateFormat);
        }

        var date = root.Element(CiiDocument)?.Element(CiiIssueDateTime)?.Element(CiiDateTimeString);

        return date?.Attribute(CiiDateFormatAttribute)?.Value == CiiDateFormatCode
            ? ParseDate(date.Value, CiiDateFormat)
            : null;
    }

    /// <summary>The profile the XMP metadata of a PDF declares, trimmed; none when it declares none.</summary>
    /// <remarks>
    /// The profile is <c>ConformanceLevel</c> of the Factur-X extension schema
    /// or of the deprecated ZUGFeRD 2.0 one, written as an element or as an
    /// attribute; both forms occur.
    /// </remarks>
    /// <param name="xmp">The XMP metadata.</param>
    public static string? DeclaredProfileOf(XDocument xmp)
    {
        ArgumentNullException.ThrowIfNull(xmp);

        XName[] names = [XName.Get(ConformanceLevel, FacturXXmpNamespace), XName.Get(ConformanceLevel, ZugferdXmpNamespace)];
        var value = xmp.Descendants()
            .Select(element => names.Select(name => element.Element(name)?.Value ?? element.Attribute(name)?.Value).FirstOrDefault(found => found is not null))
            .FirstOrDefault(found => found is not null)
            ?.Trim();

        return string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>A date in the given exact format, none when it is not.</summary>
    private static DateOnly? ParseDate(string? value, string format) =>
        DateOnly.TryParseExact(value?.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;
}
