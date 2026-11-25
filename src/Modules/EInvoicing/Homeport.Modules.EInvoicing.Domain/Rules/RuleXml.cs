// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Globalization;
using System.Xml.Linq;

namespace Homeport.Modules.EInvoicing.Domain.Rules;

/// <summary>
/// The XPath semantics business rules rely on where they check the document:
/// whitespace, booleans, numbers and dates compared the way the schematron of
/// EN 16931 compares them.
/// </summary>
internal static class RuleXml
{
    /// <summary>The namespace of UBL's basic components.</summary>
    public static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";

    /// <summary>The namespace of UBL's aggregate components.</summary>
    public static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";

    /// <summary>The namespace of the CII root element.</summary>
    public static readonly XNamespace Rsm = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";

    /// <summary>The namespace of CII's aggregate entities.</summary>
    public static readonly XNamespace Ram = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";

    /// <summary>The namespace of CII's unqualified data types.</summary>
    public static readonly XNamespace Udt = "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100";

    /// <summary>The attribute that names the format of a CII date.</summary>
    public static readonly XName FormatAttribute = "format";

    /// <summary>The attribute that names the currency of an amount.</summary>
    public static readonly XName CurrencyIdAttribute = "currencyID";

    /// <summary>The attribute that names the scheme of an identifier.</summary>
    public static readonly XName SchemeIdAttribute = "schemeID";

    /// <summary>The attribute that names the code list of a code.</summary>
    public static readonly XName ListIdAttribute = "listID";

    /// <summary>The attribute that names the unit of a quantity.</summary>
    public static readonly XName UnitCodeAttribute = "unitCode";

    /// <summary>Date format 102 (UNTDID 2379), <c>CCYYMMDD</c>, the one EN 16931 allows in CII.</summary>
    public const string DateFormat102 = "102";

    /// <summary>The tax scheme, and the tax type in CII, of VAT.</summary>
    public const string VatScheme = "VAT";

    /// <summary>The scheme a CII tax registration takes for a VAT identifier.</summary>
    public const string CiiVatRegistrationScheme = "VA";

    /// <summary>The VAT category code (UNCL 5305) for services outside the scope of tax, the one without a rate.</summary>
    public const string NotSubjectToVat = "O";

    /// <summary>The payment means type codes (UNCL 4461) of a credit transfer and of a SEPA credit transfer.</summary>
    public static readonly string[] CreditTransferCodes = ["30", "58"];

    // The four characters XPath counts as whitespace; .NET counts more.
    private static readonly char[] XPathWhitespace = [' ', '\t', '\r', '\n'];

    // The forms of xs:date: without a timezone, in UTC, or with an offset.
    private static readonly string[] XsDateFormats = ["yyyy-MM-dd", "yyyy-MM-dd'Z'", "yyyy-MM-ddzzz"];

    /// <summary>The value as XPath's <c>normalize-space</c> returns it; empty for none.</summary>
    /// <param name="value">The value, if there is one.</param>
    public static string NormalizeSpace(string? value) =>
        value is null ? string.Empty : string.Join(' ', value.Split(XPathWhitespace, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>Whether <c>normalize-space</c> of the element is empty, as it is for an element that is absent.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    public static bool IsBlank(XElement? element) => NormalizeSpace(element?.Value).Length == 0;

    /// <summary>Whether <c>normalize-space</c> of the attribute is empty, as it is for an attribute that is absent.</summary>
    /// <param name="attribute">The attribute, if the element carries it.</param>
    public static bool IsBlank(XAttribute? attribute) => NormalizeSpace(attribute?.Value).Length == 0;

    /// <summary>Whether an element holds <paramref name="value"/> as an XML Schema boolean, as <c>cbc:ChargeIndicator = true()</c> compares it.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    /// <param name="value">The boolean compared with.</param>
    public static bool IsBoolean(XElement? element, bool value) =>
        NormalizeSpace(element?.Value) is var text && (value ? text is "true" or "1" : text is "false" or "0");

    /// <summary>Whether any of the elements holds a number of at least 0, as a general comparison <c>&gt;= 0</c> tests it.</summary>
    /// <param name="elements">The elements; none makes the comparison false.</param>
    public static bool AnyIsNotNegative(IEnumerable<XElement> elements) =>
        elements.Any(element => decimal.TryParse(element.Value.Trim(XPathWhitespace), NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number >= 0);

    /// <summary>Whether any end date lies on or after any start date, the dates as xs:date compares them.</summary>
    /// <param name="ends">The end dates.</param>
    /// <param name="starts">The start dates.</param>
    public static bool AnyDateOnOrAfter(IEnumerable<XElement> ends, IEnumerable<XElement> starts)
    {
        var startDates = starts.Select(XsDate).OfType<DateTimeOffset>().ToList();

        return ends.Select(XsDate).OfType<DateTimeOffset>().Any(end => startDates.Any(start => end >= start));
    }

    /// <summary>Whether any of the first values is, as a string, on or after any of the second, as a general comparison of untyped values tests it.</summary>
    /// <param name="first">The first values.</param>
    /// <param name="second">The second values.</param>
    public static bool AnyStringOnOrAfter(IEnumerable<string> first, IEnumerable<string> second)
    {
        var others = second.ToList();

        return first.Any(value => others.Any(other => string.CompareOrdinal(value, other) >= 0));
    }

    /// <summary>The number of characters XPath's <c>string-length</c> counts.</summary>
    /// <param name="value">The value.</param>
    public static int StringLength(string value) => value.EnumerateRunes().Count();

    /// <summary>Whether the UBL element names the tax scheme VAT, as <c>cac:TaxScheme/normalize-space(upper-case(cbc:ID)) = 'VAT'</c> tests it.</summary>
    /// <param name="element">A tax category or party tax scheme.</param>
    public static bool IsUblVat(XElement element) =>
        element.Elements(Cac + "TaxScheme").Any(scheme => NormalizeSpace(scheme.Element(Cbc + "ID")?.Value.ToUpperInvariant()) == VatScheme);

    /// <summary>Whether the CII element names the tax type VAT, as <c>upper-case(ram:TypeCode) = 'VAT'</c> tests it.</summary>
    /// <param name="element">A trade tax.</param>
    public static bool IsCiiVat(XElement element) =>
        element.Elements(Ram + "TypeCode").Any(code => code.Value.ToUpperInvariant() == VatScheme);

    /// <summary>Whether the element's value is one of <paramref name="values"/>, compared exactly, as XPath compares an untyped value with a string.</summary>
    /// <param name="element">The element, if the document carries it.</param>
    /// <param name="values">The values compared with.</param>
    public static bool IsOneOf(XElement? element, params string[] values) => element is not null && values.Contains(element.Value, StringComparer.Ordinal);

    /// <summary>The position of <paramref name="element"/> in <paramref name="elements"/>; -1 when it is not among them.</summary>
    /// <param name="elements">The elements.</param>
    /// <param name="element">The element looked for.</param>
    public static int IndexOf(IReadOnlyList<XElement> elements, XElement element)
    {
        for (var index = 0; index < elements.Count; index++)
        {
            if (elements[index] == element)
            {
                return index;
            }
        }

        return -1;
    }

    /// <summary>The instant an xs:date starts; none for a value that is no xs:date. Without a timezone, UTC is assumed.</summary>
    private static DateTimeOffset? XsDate(XElement element) =>
        DateTimeOffset.TryParseExact(
            element.Value.Trim(XPathWhitespace),
            XsDateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var date)
            ? date
            : null;
}
