// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml;
using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Documents;
using Homeport.Shared.Contracts;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Homeport.Modules.EInvoicing.Application.Documents;

/// <summary>Takes the invoice a ZUGFeRD / Factur-X PDF carries out of it.</summary>
/// <remarks>
/// The invoice is found by its file name alone. The PDF is not checked for
/// PDF/A conformance, and the relationship of the file to the document is not
/// looked at.
/// </remarks>
internal static class PdfInvoiceExtraction
{
    // XMP is XML a sender wrote: read with the same care as an invoice.
    private static readonly XmlReaderSettings XmpSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
    };

    /// <summary>Every PDF starts with this header (ISO 32000-2, 7.5.2).</summary>
    private static ReadOnlySpan<byte> PdfHeader => "%PDF-"u8;

    /// <summary>The invoice <paramref name="pdf"/> carries and the profile it declares, or why there is none.</summary>
    /// <param name="pdf">The PDF as it was sent.</param>
    public static Result<EmbeddedInvoice> Extract(byte[] pdf)
    {
        if (!pdf.AsSpan().StartsWith(PdfHeader))
        {
            return Result<EmbeddedInvoice>.Failure(DocumentRules.Unreadable("The document does not start with a PDF header."));
        }

        IReadOnlyList<EmbeddedFile> files;
        string? declaredProfile;

        try
        {
            using var document = PdfDocument.Open(pdf);
            files = document.Advanced.TryGetEmbeddedFiles(out var embedded) ? embedded : [];

            declaredProfile = document.TryGetXmpMetadata(out var xmp) ? DeclaredProfileOf(xmp) : null;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // The PDF is untrusted input, and its parser throws a range of
            // exceptions for a broken one: an expected outcome, so a result.
            return Result<EmbeddedInvoice>.Failure(DocumentRules.Unreadable($"The document cannot be read as a PDF: {exception.Message}"));
        }

        foreach (var name in DocumentRules.EmbeddedInvoiceNames)
        {
            var file = files.FirstOrDefault(candidate => NameOf(candidate) == name);
            if (file is not null)
            {
                return Result<EmbeddedInvoice>.Success(new EmbeddedInvoice(name, [.. file.Bytes], declaredProfile));
            }
        }

        return Result<EmbeddedInvoice>.Failure(DocumentRules.PdfWithoutInvoice());
    }

    /// <summary>The file name of an embedded file: the file specification's, else its key in the name tree.</summary>
    private static string NameOf(EmbeddedFile file) =>
        string.IsNullOrEmpty(file.FileSpecification) ? file.Name : file.FileSpecification;

    /// <summary>The profile XMP metadata declares; none when it cannot be read, which is left to a PDF/A check.</summary>
    private static string? DeclaredProfileOf(XmpMetadata xmp)
    {
        try
        {
            using var stream = new MemoryStream(xmp.GetXmlBytes().ToArray(), writable: false);
            using var reader = XmlReader.Create(stream, XmpSettings);

            return DocumentRules.DeclaredProfileOf(XDocument.Load(reader));
        }
        catch (XmlException)
        {
            // Unreadable XMP declares nothing; the module does not check PDF/A.
            return null;
        }
    }
}

/// <summary>The invoice a PDF carries.</summary>
/// <param name="FileName">The name it is embedded under.</param>
/// <param name="Invoice">The invoice, as it is embedded.</param>
/// <param name="DeclaredProfile">The profile the XMP metadata of the PDF declares; none when it declares none.</param>
internal sealed record EmbeddedInvoice(string FileName, byte[] Invoice, string? DeclaredProfile);
