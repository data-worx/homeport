// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Xml.Linq;
using Homeport.Modules.EInvoicing.Domain.Model;

namespace Homeport.Modules.EInvoicing.Domain.Reading;

/// <summary>Reads the values of business terms out of the elements of either syntax.</summary>
internal static class XmlValues
{
    private static readonly XName SchemeIdAttribute = "schemeID";
    private static readonly XName ListIdAttribute = "listID";
    private static readonly XName SchemeVersionIdAttribute = "schemeVersionID";
    private static readonly XName ListVersionIdAttribute = "listVersionID";
    private static readonly XName MimeCodeAttribute = "mimeCode";
    private static readonly XName FilenameAttribute = "filename";

    /// <summary>A text, exactly as the element holds it; none when it is absent or blank.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    public static string? Text(XElement? element) => Text(element?.Value);

    /// <summary>A text, exactly as it is written; none when it is absent or blank.</summary>
    /// <param name="value">The text, if the document carries it.</param>
    public static string? Text(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>A code, an amount, a quantity, a percentage or a reference, trimmed; none when it is absent or blank.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    public static string? Token(XElement? element) => Trimmed(element?.Value);

    /// <summary>A value an attribute holds, trimmed; none when it is absent or blank.</summary>
    /// <param name="attribute">The attribute, if the element carries it.</param>
    public static string? Token(XAttribute? attribute) => Trimmed(attribute?.Value);

    /// <summary>An identifier with the scheme and scheme version the element names; none when it holds no value.</summary>
    /// <remarks>
    /// The scheme is <c>schemeID</c>, or <c>listID</c> for an identifier
    /// written as a code, such as an item classification.
    /// </remarks>
    /// <param name="element">The element, if the document carries it.</param>
    public static Identifier? Identifier(XElement? element) => Token(element) is { } value
        ? new Identifier(
            value,
            Token(element!.Attribute(SchemeIdAttribute)) ?? Token(element.Attribute(ListIdAttribute)),
            Token(element.Attribute(SchemeVersionIdAttribute)) ?? Token(element.Attribute(ListVersionIdAttribute)))
        : null;

    /// <summary>An identifier with the given scheme, or none; for an element whose <c>schemeID</c> belongs to the syntax binding.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    /// <param name="schemeId">The scheme, where the syntax writes it elsewhere.</param>
    public static Identifier? Identifier(XElement? element, string? schemeId) =>
        Token(element) is { } value ? new Identifier(value, schemeId) : null;

    /// <summary>The metadata of a binary object; none when the document carries none. Its content is never read.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    public static BinaryObject? BinaryObject(XElement? element) => element is null
        ? null
        : new BinaryObject(Token(element.Attribute(MimeCodeAttribute)), Token(element.Attribute(FilenameAttribute)));

    /// <summary>A date in the given exact format; none when it is absent or written otherwise.</summary>
    /// <param name="value">The value, trimmed by the caller or not.</param>
    /// <param name="format">The format the syntax writes dates in.</param>
    public static DateOnly? Date(string? value, string format) =>
        DateOnly.TryParseExact(value?.Trim(), format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    /// <summary>Whether an element holds <c>true</c> as an XML Schema boolean.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    public static bool IsTrue(XElement? element) => Token(element) is "true" or "1";

    /// <summary>Whether an element holds <c>false</c> as an XML Schema boolean.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    public static bool IsFalse(XElement? element) => Token(element) is "false" or "0";

    /// <summary>A value, trimmed; none when it is absent or blank.</summary>
    private static string? Trimmed(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
