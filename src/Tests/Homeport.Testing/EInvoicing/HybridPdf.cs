// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Text;

namespace Homeport.Testing.EInvoicing;

/// <summary>
/// Writes a minimal PDF that carries files the way a ZUGFeRD / Factur-X PDF
/// does: in the EmbeddedFiles name tree and the AF array of the catalog, with
/// XMP metadata next to them. It is no PDF/A; the module does not check that.
/// </summary>
/// <remarks>
/// FeRD's own sample PDFs may not be copied into the repository, so the tests
/// build their own.
/// </remarks>
public static class HybridPdf
{
    public const string FacturXXmpNamespace = "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#";

    public const string ZugferdXmpNamespace = "urn:zugferd:pdfa:CrossIndustryDocument:invoice:1p0#";

    /// <summary>A PDF carrying <paramref name="invoice"/> as <paramref name="fileName"/>, declaring <paramref name="profile"/> in its XMP metadata.</summary>
    public static byte[] Carrying(string fileName, string invoice, string? profile = null, string xmpNamespace = FacturXXmpNamespace) =>
        Create([(fileName, Encoding.UTF8.GetBytes(invoice))], profile is null ? null : Xmp(profile, fileName, xmpNamespace));

    /// <summary>A PDF carrying the given files and, if given, the XMP metadata.</summary>
    public static byte[] Create(IReadOnlyList<(string Name, byte[] Content)> files, string? xmp)
    {
        ArgumentNullException.ThrowIfNull(files);

        // Objects: 1 catalog, 2 pages, 3 page, 4 metadata, then a file
        // specification and its embedded file stream per file.
        var objects = new List<byte[]>();
        var specifications = files.Select((_, index) => 5 + (2 * index)).ToList();
        var names = string.Join(' ', files.Select((file, index) => $"({file.Name}) {specifications[index]} 0 R"));
        var references = string.Join(' ', specifications.Select(number => $"{number} 0 R"));
        var metadata = xmp is null ? string.Empty : " /Metadata 4 0 R";

        objects.Add(Ascii($"<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [{names}] >> >> /AF [{references}]{metadata} >>"));
        objects.Add(Ascii("<< /Type /Pages /Kids [3 0 R] /Count 1 >>"));
        objects.Add(Ascii("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] >>"));
        objects.Add(Stream("/Type /Metadata /Subtype /XML", Encoding.UTF8.GetBytes(xmp ?? string.Empty)));

        foreach (var (file, index) in files.Select((file, index) => (file, index)))
        {
            objects.Add(Ascii($"<< /Type /Filespec /F ({file.Name}) /UF ({file.Name}) /EF << /F {specifications[index] + 1} 0 R >> /AFRelationship /Alternative >>"));
            objects.Add(Stream("/Type /EmbeddedFile /Subtype /text#2Fxml", file.Content));
        }

        using var pdf = new MemoryStream();
        Write(pdf, "%PDF-1.7\n");

        var offsets = new List<long>();
        foreach (var (body, number) in objects.Select((body, index) => (body, index + 1)))
        {
            offsets.Add(pdf.Position);
            Write(pdf, $"{number} 0 obj\n");
            pdf.Write(body);
            Write(pdf, "\nendobj\n");
        }

        var xref = pdf.Position;
        Write(pdf, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets)
        {
            Write(pdf, $"{offset.ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
        }

        Write(pdf, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");

        return pdf.ToArray();
    }

    /// <summary>XMP metadata declaring <paramref name="profile"/> in the given extension schema.</summary>
    public static string Xmp(string profile, string fileName, string xmpNamespace = FacturXXmpNamespace) => $"""
        <?xpacket begin="" id="W5M0MpCehiHzreSzNTczkc9d"?>
        <x:xmpmeta xmlns:x="adobe:ns:meta/">
          <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#">
            <rdf:Description rdf:about="" xmlns:fx="{xmpNamespace}">
              <fx:DocumentType>INVOICE</fx:DocumentType>
              <fx:DocumentFileName>{fileName}</fx:DocumentFileName>
              <fx:Version>1.0</fx:Version>
              <fx:ConformanceLevel>{profile}</fx:ConformanceLevel>
            </rdf:Description>
          </rdf:RDF>
        </x:xmpmeta>
        <?xpacket end="w"?>
        """;

    private static byte[] Stream(string dictionary, byte[] content)
    {
        using var stream = new MemoryStream();
        Write(stream, $"<< {dictionary} /Length {content.Length} >>\nstream\n");
        stream.Write(content);
        Write(stream, "\nendstream");

        return stream.ToArray();
    }

    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static void Write(Stream stream, string text) => stream.Write(Encoding.ASCII.GetBytes(text));
}
