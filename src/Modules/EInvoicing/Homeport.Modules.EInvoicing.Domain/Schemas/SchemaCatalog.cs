// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Xml;
using System.Xml.Schema;

namespace Homeport.Modules.EInvoicing.Domain.Schemas;

/// <summary>The XML schemas of the syntaxes EN 16931 binds to, UBL 2.1 and CII D16B, compiled once.</summary>
/// <remarks>
/// The schemas are embedded in the module unchanged, as the KoSIT validator
/// configuration for XRechnung ships them; their publishers forbid changing
/// them. They are versioned with the module; an operator cannot override them.
/// </remarks>
public sealed class SchemaCatalog
{
    // The prefix of the logical names the project file gives the schemas,
    // followed by their path below Schemas/.
    private const string ResourcePrefix = "Homeport.Modules.EInvoicing.Domain.Schemas/";

    // The schemas are addressed under a scheme of their own, so an import
    // resolves among the embedded files and never reaches the network or
    // the file system.
    private const string EmbeddedScheme = "embedded";

    private static readonly string[] EntryPoints =
    [
        "ubl-2.1/maindoc/UBL-Invoice-2.1.xsd",
        "ubl-2.1/maindoc/UBL-CreditNote-2.1.xsd",
        "cii-d16b/CrossIndustryInvoice_100pD16B.xsd",
    ];

    /// <summary>Creates a catalog of compiled schemas.</summary>
    /// <param name="schemas">The compiled schemas.</param>
    private SchemaCatalog(XmlSchemaSet schemas) => Schemas = schemas;

    /// <summary>The compiled schemas of every syntax, read-only from here on.</summary>
    public XmlSchemaSet Schemas { get; }

    /// <summary>The schemas embedded in the module, compiled.</summary>
    /// <exception cref="InvalidOperationException">A schema is missing or does not compile, a defect of the build.</exception>
    public static SchemaCatalog LoadEmbedded()
    {
        var resolver = new EmbeddedSchemaResolver();
        var schemas = new XmlSchemaSet { XmlResolver = resolver };

        try
        {
            foreach (var entryPoint in EntryPoints)
            {
                var uri = EmbeddedSchemaResolver.UriOf(entryPoint);
                using var reader = XmlReader.Create(resolver.Open(uri), EmbeddedSchemaResolver.ReaderSettings(resolver), uri.AbsoluteUri);
                schemas.Add(targetNamespace: null, reader);
            }

            schemas.Compile();
        }
        catch (Exception exception) when (exception is XmlException or XmlSchemaException)
        {
            throw new InvalidOperationException("The embedded XML schemas do not compile.", exception);
        }

        return new SchemaCatalog(schemas);
    }

    /// <summary>Resolves the URIs of the schemas to the files embedded in the module, and nothing else.</summary>
    private sealed class EmbeddedSchemaResolver : XmlResolver
    {
        /// <summary>The URI of an embedded schema.</summary>
        /// <param name="path">Its path below <c>Schemas/</c>.</param>
        public static Uri UriOf(string path) => new($"{EmbeddedScheme}:///{path}");

        /// <summary>The settings a schema is read with.</summary>
        /// <remarks>
        /// The copy of the XML Signature schema in UBL declares its entities in
        /// an internal DTD subset, so a DTD is parsed here, unlike in an invoice.
        /// That is safe: only the embedded schemas are read this way, and the
        /// resolver reaches nothing outside them.
        /// </remarks>
        /// <param name="resolver">The resolver for whatever a schema refers to.</param>
        public static XmlReaderSettings ReaderSettings(XmlResolver resolver) => new()
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = resolver,
        };

        /// <summary>The embedded schema at <paramref name="uri"/>.</summary>
        /// <param name="uri">An absolute URI under the embedded scheme.</param>
        public Stream Open(Uri uri)
        {
            if (uri.Scheme != EmbeddedScheme)
            {
                throw new XmlException($"The schema '{uri}' is not embedded in the module.");
            }

            var name = ResourcePrefix + uri.AbsolutePath.TrimStart('/');

            return typeof(SchemaCatalog).Assembly.GetManifestResourceStream(name)
                ?? throw new XmlException($"The module carries no embedded schema '{name}'.");
        }

        /// <inheritdoc/>
        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            ArgumentNullException.ThrowIfNull(absoluteUri);

            // An imported schema is read like an entry point, so the XML
            // Signature schema's DTD subset is parsed wherever it is imported.
            return ofObjectToReturn == typeof(XmlReader)
                ? XmlReader.Create(Open(absoluteUri), ReaderSettings(this), absoluteUri.AbsoluteUri)
                : Open(absoluteUri);
        }
    }
}
